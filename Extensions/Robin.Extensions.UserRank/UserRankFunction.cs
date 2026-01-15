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
using Robin.Extensions.UserRank.Drawing;
using Robin.Middlewares.Fluent;
using Robin.Middlewares.Fluent.Event;
using SkiaSharp;

namespace Robin.Extensions.UserRank;

[BotFunctionInfo("user_rank", "用户发言排行")]
public partial class UserRankFunction(FunctionContext<UserRankOption> context)
    : BotFunction<UserRankOption>(context),
        IFluentFunction
{
    private RankImage Image = null!;
    private readonly SemaphoreSlim _drawingSemaphore = new(1, 1);

    private async Task<bool> SendUserRankAsync(
        long groupId,
        int n,
        long? userId,
        CancellationToken token
    )
    {
        n = n > 0 ? n : _context.Configuration.TopN;

        var members = await GetGroupMembersAsync(groupId, token);
        var peopleCount = members.Count(member => member.Count is > 0);
        var messageCount = (uint)members.Sum(member => member.Count);

        var currentRank = members
            .Where(member => member.Count is > 0)
            .OrderByDescending(member => member.Count)
            .ThenBy(member => member.Timestamp)
            .ToList();

        var prevRank = members
            .Where(member => member.PrevCount is > 0)
            .OrderByDescending(member => member.PrevCount)
            .ThenBy(member => member.PrevTimestamp)
            .Index()
            .ToDictionary(pair => pair.Item, pair => pair.Index);

        var delta = currentRank
            .Select(
                (member, index) =>
                    (prevRank.TryGetValue(member, out var rank) ? rank : prevRank.Count) - index
            )
            .ToList();

        if (
            await new GetGroupMemberList(groupId, NoCache: true).SendAsync(_context, token)
            is not { Members: { } memberInfos }
        )
            return false;

        var dict = memberInfos.ToDictionary(
            member => member.UserId,
            member =>
                member.Card switch
                {
                    null or "" => member.Nickname,
                    _ => member.Card,
                }
        );

        if (
            await new GetGroupInfo(groupId, NoCache: true).SendAsync(_context, token)
            is not { Info.GroupName: { } groupName }
        )
            return false;

        var ranks = new List<(int rank, long userId, string name, uint count, int delta)>(n + 1);

        if (userId is not null)
        {
            int index = currentRank.FindIndex(member => member.UserId == userId);
            var name = dict.TryGetValue(userId.Value, out var value) ? value : userId.ToString();
            if (index >= 0)
                ranks.Add((index + 1, userId.Value, name!, currentRank[index].Count, delta[index]));
            else
                ranks.Add((currentRank.Count + 1, userId.Value, name!, 0, 0));
        }

        ranks.AddRange(
            currentRank
                .Take(n)
                .Select(
                    (member, index) =>
                    {
                        var name = dict.TryGetValue(member.UserId, out var value)
                            ? value
                            : member.UserId.ToString();
                        return (index + 1, member.UserId, name!, member.Count, delta[index]);
                    }
                )
        );

        using var image = await _drawingSemaphore.ConsumeAsync(
            () => Image.GenerateAsync(groupName, groupId, peopleCount, messageCount, ranks, token),
            token
        );
        using var data = image.Encode(SKEncodedImageFormat.Webp, 100);

        await new SendGroupMessage(
            groupId,
            [new ImageData("base64://" + Convert.ToBase64String(data.AsSpan()))]
        ).SendAsync(_context, token);

        return true;
    }

    [GeneratedRegex(@"^/rank(?:\s+(?<n>\d+))?$")]
    private static partial Regex RankRegex { get; }

    public async Task OnCreatingAsync(FunctionBuilder builder, CancellationToken token)
    {
        await CreateTableAsync(token);

        Image = new(
            _context.Configuration.FontPaths,
            _context.Configuration.ColorPalette,
            _context.Configuration.CardWidth,
            _context.Configuration.CardHeight,
            _context.Configuration.CardGap,
            _context.Configuration.PrimaryFontSize
        );

        builder
            .On<GroupMessageEvent>("collect message")
            .AsIntrinsic()
            .Do(ctx =>
                IncreaseCountAsync(ctx.Event.GroupId, ctx.Event.UserId, ctx.Event.Time, ctx.Token)
            )
            .On<GroupMessageEvent>("show rank")
            .OnRegex(RankRegex)
            .DoExpensive(
                async tuple =>
                {
                    var (ctx, match) = tuple;
                    var (e, t) = ctx;

                    return await SendUserRankAsync(
                        e.GroupId,
                        match.Groups["n"] switch
                        {
                            { Success: true, Value: { } value } => int.Min(int.Parse(value), 50),
                            _ => 0,
                        },
                        e.UserId,
                        t
                    );
                },
                tuple => tuple.EventContext,
                _context
            )
            .OnCron("0 0 0 * * ?", "rank cron")
            .Do(async token =>
            {
                try
                {
                    await Task.WhenAll(
                        (await GetGroupsAsync(token)).Select(group =>
                            SendUserRankAsync(group, n: 0, userId: null, token)
                        )
                    );
                    await ClearAsync(token);
                }
                catch (Exception e)
                {
                    LogExceptionOccurred(_context.Logger, e);
                }
            });
    }

    public override async Task StopAsync(CancellationToken token)
    {
        _drawingSemaphore.Dispose();
        _dbSemaphore.Dispose();
        await _db.DisposeAsync();
    }
}

// DB
public partial class UserRankFunction
{
    private readonly UserRankDbContext _db = new(context.BotContext.Uin);
    private readonly SemaphoreSlim _dbSemaphore = new(1, 1);

    private Task<bool> CreateTableAsync(CancellationToken token) =>
        _dbSemaphore.ConsumeAsync(() => _db.Database.EnsureCreatedAsync(token), token);

    private Task<int> IncreaseCountAsync(
        long groupId,
        long userId,
        long timestamp,
        CancellationToken token
    ) =>
        _dbSemaphore.ConsumeAsync(
            () =>
            {
                if (_db.Members.Find(groupId, userId) is not { } member)
                    _db.Members.Add(
                        new Member
                        {
                            GroupId = groupId,
                            UserId = userId,
                            Count = 1,
                            Timestamp = timestamp,
                            PrevCount = 0,
                            PrevTimestamp = 0,
                        }
                    );
                else
                {
                    member.Count++;
                    member.Timestamp = timestamp;
                }
                return _db.SaveChangesAsync(token);
            },
            token
        );

    private Task<List<long>> GetGroupsAsync(CancellationToken token) =>
        _dbSemaphore.ConsumeAsync(
            () =>
                _db
                    .Members.AsNoTracking()
                    .Where(member => member.Count > 0)
                    .Select(member => member.GroupId)
                    .Distinct()
                    .ToListAsync(token),
            token
        );

    private Task<List<Member>> GetGroupMembersAsync(long groupId, CancellationToken token) =>
        _dbSemaphore.ConsumeAsync(
            () =>
                _db
                    .Members.AsNoTracking()
                    .Where(member => member.GroupId == groupId)
                    .ToListAsync(token),
            token
        );

    private Task<int> ClearAsync(CancellationToken token) =>
        _dbSemaphore.ConsumeAsync(
            async Task<int> () =>
            {
                var inactiveMembers = await _db
                    .Members.Where(member => member.Count == 0)
                    .ToListAsync(token);
                _db.Members.RemoveRange(inactiveMembers);

                var activeMembers = await _db
                    .Members.Where(member => member.Count > 0)
                    .ToListAsync(token);
                activeMembers.ForEach(member =>
                {
                    member.PrevCount = member.Count;
                    member.PrevTimestamp = member.Timestamp;
                    member.Count = 0;
                    member.Timestamp = 0;
                });
                return await _db.SaveChangesAsync(token);
            },
            token
        );
}

// Log
public partial class UserRankFunction
{
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Exception occurred while sending word cloud"
    )]
    private static partial void LogExceptionOccurred(ILogger logger, Exception exception);
}
