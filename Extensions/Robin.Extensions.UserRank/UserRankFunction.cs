using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Robin.Abstractions;
using Robin.Abstractions.Context;
using Robin.Abstractions.Event.Message;
using Robin.Abstractions.Message.Entity;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Requests;
using Robin.Abstractions.Operation.Responses;
using Robin.Annotations.Cron;
using Robin.Fluent;
using Robin.Fluent.Builder;

namespace Robin.Extensions.UserRank;

[BotFunctionInfo("user_rank", "用户发言排行")]
[OnCron("0 0 0 * * ?")]
// ReSharper disable once UnusedType.Global
public partial class UserRankFunction(FunctionContext context) : BotFunction(context), ICronHandler, IFluentFunction
{
    private UserRankOption? _option;

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

    private async Task SendUserRankAsync(long groupId, int n = 0, bool clear = false, CancellationToken token = default)
    {
        var peopleCount = await GetGroupPeopleCountAsync(groupId, token);
        var msgCount = await GetGroupMessageCountAsync(groupId, token);
        var top = await GetGroupTopNAsync(groupId, n > 0 ? n : _option!.TopN, token);
        if (clear) await ClearGroupMessagesAsync(groupId, token);

        string message;

        if (peopleCount is 0 || msgCount is 0) message = "本群暂无发言记录";
        else
        {
            if (await new GetGroupMemberListRequest(groupId, true)
                .SendAsync<GetGroupMemberListResponse>(_context.OperationProvider, _context.Logger, token)
                is not { Members: { } members })
                return;

            var dict = members
                .Select(member => (
                    member.UserId,
                    Name: member.Card switch
                    {
                        null or "" => member.Nickname,
                        _ => member.Card
                    }
                ))
                .ToDictionary(pair => pair.UserId, pair => pair.Name);

            var stringBuilder = new StringBuilder(
                $"""
                本群 {peopleCount} 位朋友共产生 {msgCount} 条发言
                活跃用户排行榜

                """
            );

            stringBuilder.AppendJoin('\n', top.Select(
                pair => $"{(dict.TryGetValue(pair.Id, out var value) ? value : pair.Id)} 贡献：{pair.Count}"
            ));
            message = stringBuilder.ToString();
        }

        await new SendGroupMessageRequest(groupId, [
            new TextData(message)
        ]).SendAsync(_context.OperationProvider, _context.Logger, token);
    }


    [GeneratedRegex(@"^/rank(?:\s+(?<n>\d+))?$")]
    private static partial Regex RankRegex();

    public string? Description { get; set; }

    public async Task OnCreatingAsync(FunctionBuilder builder, CancellationToken token)
    {
        if (_context.Configuration.Get<UserRankOption>() is not { } option)
        {
            LogOptionBindingFailed(_context.Logger);
            return;
        }

        _option = option;

        await CreateTableAsync(token);

        builder.On<GroupMessageEvent>()
            .AsAlwaysFired()
            .Do(async ctx =>
            {
                var (e, t) = ctx;
                await InsertDataAsync(e.GroupId, e.UserId, t);
            })
            .On<GroupMessageEvent>()
            .OnRegex(RankRegex())
            .Do(async tuple =>
            {
                var (ctx, match) = tuple;
                var (e, t) = ctx;

                await SendUserRankAsync(e.GroupId, match.Groups["n"] switch
                {
                    { Success: true, Value: { } value } => int.Min(int.Parse(value), 50),
                    _ => 0
                }, token: t);
            });
    }

    #region Database

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

    #endregion

    #region Log

    [LoggerMessage(EventId = 0, Level = LogLevel.Warning, Message = "Option binding failed")]
    private static partial void LogOptionBindingFailed(ILogger logger);

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Exception occurred while sending word cloud")]
    private static partial void LogExceptionOccurred(ILogger logger, Exception exception);

    #endregion
}
