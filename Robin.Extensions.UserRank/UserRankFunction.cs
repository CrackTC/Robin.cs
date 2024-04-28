using System.Collections.Frozen;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Robin.Abstractions;
using Robin.Abstractions.Communication;
using Robin.Abstractions.Event;
using Robin.Abstractions.Event.Message;
using Robin.Abstractions.Message.Entities;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Requests;
using Robin.Abstractions.Operation.Responses;
using Robin.Annotations.Cron;
using Robin.Annotations.Filters;
using Robin.Annotations.Filters.Message;

namespace Robin.Extensions.UserRank;

[BotFunctionInfo("user_rank", "daily user rank", typeof(GroupMessageEvent))]
[OnCommand("rank")]
[OnCron("0 0 0 * * ?")]
// ReSharper disable once UnusedType.Global
public partial class UserRankFunction(
    IServiceProvider service,
    long uin,
    IOperationProvider provider,
    IConfiguration configuration,
    IEnumerable<BotFunction> functions
) : BotFunction(service, uin, provider, configuration, functions), IFilterHandler, ICronHandler
{
    private UserRankOption? _option;
    private readonly ILogger<UserRankFunction> _logger = service.GetRequiredService<ILogger<UserRankFunction>>();

    private readonly UserRankDbContext _db = new(uin);
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
                .Select(group => (group.Id, group.Count));
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

    private async Task SendUserRankAsync(long groupId, bool clear = false, CancellationToken token = default)
    {
        var peopleCount = await GetGroupPeopleCountAsync(groupId, token);
        var messageCount = await GetGroupMessageCountAsync(groupId, token);
        var top = await GetGroupTopNAsync(groupId, _option!.TopN, token);
        if (clear) await ClearGroupMessagesAsync(groupId, token);

        string message;

        if (peopleCount == 0 || messageCount == 0) message = "本群暂无发言记录";
        else
        {
            if (await new GetGroupMemberListRequest(groupId, true).SendAsync(_provider, token)
                is not GetGroupMemberListResponse { Success: true, Members: not null } memberList)
            {
                LogGetGroupMemberListFailed(_logger, groupId);
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

        if (await new SendGroupMessageRequest(groupId, [new TextData(message)]).SendAsync(_provider, token) is not { Success: true })
        {
            LogSendFailed(_logger, groupId);
            return;
        }

        LogUserRankSent(_logger, groupId);
    }

    public override async Task OnEventAsync(long selfId, BotEvent @event, CancellationToken token)
    {
        if (@event is not GroupMessageEvent e) return;
        await InsertDataAsync(e.GroupId, e.UserId, token);
    }

    public override async Task StartAsync(CancellationToken token)
    {
        if (_configuration.Get<UserRankOption>() is not { } option)
        {
            LogOptionBindingFailed(_logger);
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
            await Task.WhenAll((await GetGroupsAsync(token)).Select(group => SendUserRankAsync(group, true, token)));
        }
        catch (Exception e)
        {
            LogExceptionOccurred(_logger, e);
        }
    }

    public async Task<bool> OnFilteredEventAsync(int filterGroup, long selfId, BotEvent @event, CancellationToken token)
    {
        if (@event is not GroupMessageEvent e) return false;

        await SendUserRankAsync(e.GroupId, token: token);
        return true;
    }

    #region Log

    [LoggerMessage(EventId = 0, Level = LogLevel.Warning, Message = "Option binding failed")]
    private static partial void LogOptionBindingFailed(ILogger logger);

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Get group member list failed for group {GroupId}")]
    private static partial void LogGetGroupMemberListFailed(ILogger logger, long groupId);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Send message failed for group {GroupId}")]
    private static partial void LogSendFailed(ILogger logger, long groupId);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "Word cloud sent for group {GroupId}")]
    private static partial void LogUserRankSent(ILogger logger, long groupId);

    [LoggerMessage(EventId = 4, Level = LogLevel.Warning, Message = "Exception occurred while sending word cloud")]
    private static partial void LogExceptionOccurred(ILogger logger, Exception exception);

    #endregion
}