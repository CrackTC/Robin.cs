using System.Collections.Frozen;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Robin.Abstractions;
using Robin.Abstractions.Context;
using Robin.Abstractions.Event;
using Robin.Abstractions.Event.Message;
using Robin.Abstractions.Message.Entity;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Requests;
using Robin.Abstractions.Operation.Responses;
using Robin.Annotations.Cron;
using Robin.Annotations.Filters;
using Robin.Annotations.Filters.Message;

namespace Robin.Extensions.UserRank;

[BotFunctionInfo("user_rank", "用户发言排行", typeof(GroupMessageEvent))]
[OnCommand("rank")]
[OnCron("0 0 0 * * ?")]
// ReSharper disable once UnusedType.Global
public partial class UserRankFunction(FunctionContext context) : BotFunction(context), IFilterHandler, ICronHandler
{
    private UserRankOption? _option;

    private readonly UserRankDbContext _db = new(context.Uin);
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private Task<bool> CreateTableAsync(CancellationToken token) => _db.Database.EnsureCreatedAsync(token);

    private async Task InsertDataAsync(long groupId, long userId, CancellationToken token)
    {
        await _semaphore.WaitAsync(token);
        try
        {
            await _db.Records.AddAsync(new Record
            {
                GroupId = groupId,
                UserId = userId
            }, token);
            await _db.SaveChangesAsync(token);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<IEnumerable<long>> GetGroupsAsync(CancellationToken token)
    {
        await _semaphore.WaitAsync(token);
        try
        {
            return _db.Records.Select(record => record.GroupId).Distinct();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<int> GetGroupPeopleCountAsync(long groupId, CancellationToken token)
    {
        await _semaphore.WaitAsync(token);

        try
        {
            return await _db.Records
                .Where(record => record.GroupId == groupId)
                .GroupBy(record => record.UserId)
                .CountAsync(token);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<int> GetGroupMessageCountAsync(long groupId, CancellationToken token)
    {
        await _semaphore.WaitAsync(token);
        try
        {
            return await _db.Records
                .Where(record => record.GroupId == groupId)
                .CountAsync(token);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<IEnumerable<(long Id, int Count)>> GetGroupTopNAsync(long groupId, int n,
        CancellationToken token)
    {
        await _semaphore.WaitAsync(token);
        try
        {
            return _db.Records
                .Where(record => record.GroupId == groupId)
                .GroupBy(record => record.UserId)
                .Select(group => new
                {
                    Id = group.Key,
                    Count = group.Count()
                })
                .OrderByDescending(group => group.Count)
                .Take(n)
                .AsEnumerable()
                .Select(group => (group.Id, group.Count))
                .ToList();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task ClearGroupMessagesAsync(long groupId, CancellationToken token)
    {
        await _semaphore.WaitAsync(token);
        try
        {
            _db.Records.RemoveRange(_db.Records.Where(record => record.GroupId == groupId));
            await _db.SaveChangesAsync(token);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task SendUserRankAsync(long groupId, int n = 0, bool clear = false, CancellationToken token = default)
    {
        var peopleCount = await GetGroupPeopleCountAsync(groupId, token);
        var messageCount = await GetGroupMessageCountAsync(groupId, token);
        var top = await GetGroupTopNAsync(groupId, n > 0 ? n : _option!.TopN, token);
        if (clear) await ClearGroupMessagesAsync(groupId, token);

        string message;

        if (peopleCount == 0 || messageCount == 0) message = "本群暂无发言记录";
        else
        {
            if (await new GetGroupMemberListRequest(groupId, true).SendAsync(_context.OperationProvider, token)
                is not GetGroupMemberListResponse { Success: true, Members: not null } memberList)
            {
                LogGetGroupMemberListFailed(_context.Logger, groupId);
                return;
            }

            var dict = memberList.Members
                .Select(member => (member.UserId,
                    Name: string.IsNullOrEmpty(member.Card) ? member.Nickname : member.Card))
                .ToFrozenDictionary(pair => pair.UserId, pair => pair.Name);

            var stringBuilder = new StringBuilder($"本群 {peopleCount} 位朋友共产生 {messageCount} 条发言\n活跃用户排行榜\n");
            stringBuilder.AppendJoin('\n', top.Select(pair => $"{(dict.TryGetValue(pair.Id, out var value) ? value : pair.Id)} 贡献：{pair.Count}"));
            message = stringBuilder.ToString();
        }

        if (await new SendGroupMessageRequest(groupId, [
                new TextData(message)
            ]).SendAsync(_context.OperationProvider, token) is not { Success: true })
        {
            LogSendFailed(_context.Logger, groupId);
            return;
        }

        LogUserRankSent(_context.Logger, groupId);
    }

    public override async Task OnEventAsync(EventContext<BotEvent> eventContext)
    {
        if (eventContext.Event is not GroupMessageEvent e) return;
        await InsertDataAsync(e.GroupId, e.UserId, eventContext.Token);
    }

    public override async Task StartAsync(CancellationToken token)
    {
        if (_context.Configuration.Get<UserRankOption>() is not { } option)
        {
            LogOptionBindingFailed(_context.Logger);
            return;
        }

        _option = option;

        await CreateTableAsync(token);
    }

    public override async Task StopAsync(CancellationToken token)
    {
        _semaphore.Dispose();
        await _db.DisposeAsync();
    }

    public async Task OnCronEventAsync(CancellationToken token)
    {
        try
        {
            await Task.WhenAll((await GetGroupsAsync(token)).Select(group => SendUserRankAsync(group, clear: true, token: token)));
        }
        catch (Exception e)
        {
            LogExceptionOccurred(_context.Logger, e);
        }
    }

    [GeneratedRegex(@"/rank\s*(\d+)?")]
    private static partial Regex RankRegex();

    public async Task<bool> OnFilteredEventAsync(int filterGroup, EventContext<BotEvent> eventContext)
    {
        if (eventContext.Event is not GroupMessageEvent e) return false;

        var match = RankRegex().Match(e.Message.OfType<TextData>().First().Text);
        if (!match.Success) return false;

        if (match.Groups.Count > 1 && int.TryParse(match.Groups[1].Value, out var n))
            await SendUserRankAsync(e.GroupId, int.Min(n, 50), token: eventContext.Token);
        else await SendUserRankAsync(e.GroupId, token: eventContext.Token);
        return true;
    }

    #region Log

    [LoggerMessage(EventId = 0, Level = LogLevel.Warning, Message = "Option binding failed")]
    private static partial void LogOptionBindingFailed(ILogger logger);

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Get group member list failed for group {GroupId}")]
    private static partial void LogGetGroupMemberListFailed(ILogger logger, long groupId);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Send message failed for group {GroupId}")]
    private static partial void LogSendFailed(ILogger logger, long groupId);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "User rank sent for group {GroupId}")]
    private static partial void LogUserRankSent(ILogger logger, long groupId);

    [LoggerMessage(EventId = 4, Level = LogLevel.Warning, Message = "Exception occurred while sending word cloud")]
    private static partial void LogExceptionOccurred(ILogger logger, Exception exception);

    #endregion
}