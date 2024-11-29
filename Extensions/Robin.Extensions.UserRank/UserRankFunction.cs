using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Robin.Abstractions;
using Robin.Abstractions.Context;
using Robin.Abstractions.Event.Message;
using Robin.Abstractions.Message.Entity;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Requests;
using Robin.Abstractions.Utility;
using Robin.Middlewares.Fluent;
using Robin.Middlewares.Fluent.Event;

namespace Robin.Extensions.UserRank;

[BotFunctionInfo("user_rank", "用户发言排行")]
public partial class UserRankFunction(
    FunctionContext<UserRankOption> context
) : BotFunction<UserRankOption>(context), IFluentFunction
{
    public override async Task StopAsync(CancellationToken token)
    {
        _semaphore.Dispose();
        await _db.DisposeAsync();
    }

    private async Task SendUserRankAsync(long groupId, int n, long? userId, CancellationToken token)
    {
        var peopleCount = await GetGroupPeopleCountAsync(groupId, token);
        var msgCount = await GetGroupMessageCountAsync(groupId, token);
        var top = await GetGroupTopNAsync(groupId, n > 0 ? n : _context.Configuration.TopN, token);
        if (userId is null) await ClearGroupMessagesAsync(groupId, token);

        string message;

        if (peopleCount is 0 || msgCount is 0) message = "本群暂无发言记录";
        else
        {
            if (await new GetGroupMemberListRequest(groupId, true).SendAsync(_context, token)
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
                pair => $"{(dict.TryGetValue(pair.Item1, out var value) ? value : pair.Item1)} 贡献：{pair.Item2}"
            ));

            if (userId is not null)
                stringBuilder.Append('\n').Append($"你的排名：{await GetUserRankAsync(groupId, userId.Value, token)}");

            message = stringBuilder.ToString();
        }

        await new SendGroupMessageRequest(groupId, [new TextData(message)]).SendAsync(_context, token);
    }


    [GeneratedRegex(@"^/rank(?:\s+(?<n>\d+))?$")]
    private static partial Regex RankRegex { get; }

    public async Task OnCreatingAsync(FunctionBuilder builder, CancellationToken token)
    {
        await CreateTableAsync(token);

        builder.On<GroupMessageEvent>("collect message")
            .AsIntrinsic()
            .Do(ctx => InsertDataAsync(ctx.Event.GroupId, ctx.Event.UserId, ctx.Token))

            .On<GroupMessageEvent>("show rank")
            .OnRegex(RankRegex)
            .Do(async tuple =>
            {
                var (ctx, match) = tuple;
                var (e, t) = ctx;

                await SendUserRankAsync(e.GroupId, match.Groups["n"] switch
                {
                    { Success: true, Value: { } value } => int.Min(int.Parse(value), 50),
                    _ => 0
                }, e.UserId, t);
            })

            .OnCron("0 0 0 * * ?", "rank cron")
            .Do(async token =>
            {
                try
                {
                    await Task.WhenAll((await GetGroupsAsync(token)).Select(group => SendUserRankAsync(group, n: 0, userId: null, token)));
                }
                catch (Exception e)
                {
                    LogExceptionOccurred(_context.Logger, e);
                }
            });
    }

    #region Database

    private readonly UserRankDbContext _db = new(context.BotContext.Uin);
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private Task<bool> CreateTableAsync(CancellationToken token) =>
        _semaphore.ConsumeAsync(() => _db.Database.EnsureCreatedAsync(token), token);

    private Task InsertDataAsync(long groupId, long userId, CancellationToken token) =>
        _semaphore.ConsumeAsync(() =>
        {
            _db.Records.Add(new Record { GroupId = groupId, UserId = userId });
            return _db.SaveChangesAsync(token);
        }, token);

    private Task<List<long>> GetGroupsAsync(CancellationToken token) =>
        _semaphore.ConsumeAsync(() => _db.Records
            .Select(record => record.GroupId)
            .Distinct()
            .ToListAsync(token), token);

    private Task<int> GetGroupPeopleCountAsync(long groupId, CancellationToken token) =>
        _semaphore.ConsumeAsync(() => _db.Records
            .Where(record => record.GroupId == groupId)
            .Select(record => record.UserId)
            .Distinct()
            .CountAsync(token), token);

    private Task<int> GetGroupMessageCountAsync(long groupId, CancellationToken token) =>
        _semaphore.ConsumeAsync(() => _db.Records
            .Where(record => record.GroupId == groupId)
            .CountAsync(token), token);

    private Task<List<Tuple<long, int>>> GetGroupTopNAsync(
        long groupId,
        int n,
        CancellationToken token
    ) =>
        _semaphore.ConsumeAsync(() => _db.Records
            .Where(record => record.GroupId == groupId)
            .GroupBy(record => record.UserId)
            .OrderByDescending(group => group.Count())
            .Select(group => Tuple.Create(group.Key, group.Count()))
            .Take(n)
            .ToListAsync(token), token);

    private Task<int> GetUserRankAsync(
        long groupId,
        long userId,
        CancellationToken token
    ) =>
        _semaphore.ConsumeAsync(async Task<int> () =>
        {
            var count = await _db.Records
                .Where(record => record.GroupId == groupId && record.UserId == userId)
                .CountAsync(token);
            return 1 + await _db.Records
                .Where(record => record.GroupId == groupId)
                .GroupBy(record => record.UserId)
                .Where(group => group.Count() > count)
                .CountAsync(token);
        }, token);

    private Task ClearGroupMessagesAsync(long groupId, CancellationToken token) =>
        _semaphore.ConsumeAsync(() =>
        {
            _db.Records.RemoveRange(_db.Records.Where(records => records.GroupId == groupId));
            return _db.SaveChangesAsync(token);
        }, token);

    #endregion

    #region Log

    [LoggerMessage(Level = LogLevel.Warning, Message = "Exception occurred while sending word cloud")]
    private static partial void LogExceptionOccurred(ILogger logger, Exception exception);

    #endregion
}
