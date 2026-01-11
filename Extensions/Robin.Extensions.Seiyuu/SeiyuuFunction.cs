using System.Text.RegularExpressions;
using Robin.Abstractions;
using Robin.Abstractions.Context;
using Robin.Abstractions.Event.Message;
using Robin.Abstractions.Message.Entity;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Requests;
using Robin.Middlewares.Fluent;
using Robin.Middlewares.Fluent.Event;
using Microsoft.Extensions.Logging;
using Robin.Abstractions.Utility;
using Microsoft.EntityFrameworkCore;

namespace Robin.Extensions.Seiyuu;

[BotFunctionInfo(FunctionName, "声优图片")]
public partial class SeiyuuFunction(
    FunctionContext<SeiyuuOption> context
) : BotFunction<SeiyuuOption>(context), IFluentFunction
{
    private const string FunctionName = "seiyuu";

    [GeneratedRegex(@"^/加图\s*(?<name>\S+)$")]
    private static partial Regex AddImageRegex { get; }

    [GeneratedRegex(@"^/添加别名\s*(?<from>\S+)\s*(?<to>\S+)$")]
    private static partial Regex AddAliasRegex { get; }

    private static readonly HttpClient _client = new() { Timeout = TimeSpan.FromMinutes(10) };

    private async Task SendReplyAsync(EventContext<MessageEvent> ctx, string message) =>
        await ((Task)ctx.Event.NewMessageRequest([
            new ReplyData(ctx.Event.MessageId),
            new TextData(message)
        ]).SendAsync(_context, ctx.Token)).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

    private static string EscapeName(string name)
    {
        return name.Trim()
            .Replace('/', '_')
            .Replace(' ', '_')
            .Replace('\\', '_')
            .Replace('.', '_');
    }

    public async Task OnCreatingAsync(FunctionBuilder builder, CancellationToken token)
    {
        await CreateTableAsync(token);

        builder.On<MessageEvent>()
            .Where(t => !_context.Configuration.BannedIds.Contains(t.Event.UserId))
            .OnRegex(AddAliasRegex)
            .DoExpensive(async t =>
            {
                var (ctx, match) = t;

                var from = EscapeName(match.Groups["from"].Value);
                var to = EscapeName(match.Groups["to"].Value);

                if (!Directory.Exists(Path.Join(FunctionName, to)))
                {
                    if (await GetAliasAsync(to, ctx.Token) is not { To: var realTo })
                    {
                        await SendReplyAsync(ctx, $"不存在文件夹或别名：{to}，无法将其作为别名目标");
                        return false;
                    }
                    to = realTo;
                }

                if (Directory.Exists(Path.Join(FunctionName, from)))
                {
                    await SendReplyAsync(ctx, $"已存在文件夹：{from}, 无法将其作为别名");
                    return false;
                }

                if (await GetAliasAsync(from, ctx.Token) is { To: var origTo })
                {
                    await SendReplyAsync(ctx, $"已存在别名 {from} -> {origTo}");
                    return false;
                }

                await AddAliasAsync(new(from, to), ctx.Token);
                await SendReplyAsync(ctx, "添加成功");
                return true;
            }, t => t.EventContext, _context)
            .On<MessageEvent>()
            .Where(t => !_context.Configuration.BannedIds.Contains(t.Event.UserId))
            .OnRegex(AddImageRegex)
            .Select(e => e.EventContext)
            .OnReply()
            .DoExpensive(async t =>
            {
                var (ctx, msgId) = t;

                var match = AddImageRegex.Match(
                    string.Join(
                        null,
                        ctx.Event.Message.OfType<TextData>()
                            .Select(data => data.Text.Trim())
                    )
                );
                var name = EscapeName(match.Groups["name"].Value);

                if (await new GetMessageRequest(msgId).SendAsync(_context, ctx.Token)
                    is not { Message.Message: { } msg })
                {
                    await SendReplyAsync(ctx, "需要引用一条包含图片的消息哦");
                    return false;
                }

                var imgs = msg.OfType<ImageData>()
                    .Where(img => img.Url is not null).ToList();
                if (imgs is [])
                {
                    await SendReplyAsync(ctx, "需要引用一条包含图片的消息哦");
                    return false;
                }

                var directory = Path.Join(FunctionName, name);
                Directory.CreateDirectory(directory);

                int successCount = 0;

                for (int i = 0; i < imgs.Count; ++i)
                {
                    var img = imgs[i];
                    var filename = Guid.NewGuid().ToString();
                    var savePath = Path.Join(directory, filename);

                    try
                    {
                        await using var stream = File.Create(savePath);
                        await using var imgStream = await _client.GetStreamAsync(img.Url);
                        await imgStream.CopyToAsync(stream);
                    }
                    catch (Exception e)
                    {
                        File.Delete(savePath);
                        await SendReplyAsync(ctx, $"保存图片{i + 1}失败：{e.Message}");
                        continue;
                    }

                    ++successCount;
                }

                if (successCount == imgs.Count)
                    await SendReplyAsync(ctx, "加图成功");

                return successCount == imgs.Count;
            }, t => t.EventContext, _context)
            .On<MessageEvent>()
            .Where(t => !_context.Configuration.BannedIds.Contains(t.Event.UserId))
            .AsFallback()
            .Where(ctx => ctx.Event.Message.All(data => data is TextData))
            .OnText()
            .Select(async t =>
            {
                var name = EscapeName(t.Text);
                if (string.IsNullOrEmpty(name)) return (t.EventContext, null);

                var path = Path.Join(FunctionName, name);
                if (Directory.Exists(path))
                {
                    if (!Directory.EnumerateFiles(path).Any()) return (t.EventContext, null);

                    if (_context.Configuration.GroupLimits.GetValueOrDefault(name) is
                        { } limits && !limits.Contains(t.EventContext.Event.SourceId))
                        return (t.EventContext, null);

                    return (t.EventContext, path);
                }

                if (await GetAliasAsync(name, t.EventContext.Token) is not { To: var to }) return (t.EventContext, null);
                path = Path.Join(FunctionName, to);
                if (Directory.Exists(path))
                {
                    if (!Directory.EnumerateFiles(path).Any()) return (t.EventContext, null);
                    if (_context.Configuration.GroupLimits.GetValueOrDefault(to) is
                        { } limits && !limits.Contains(t.EventContext.Event.SourceId))
                        return (t.EventContext, null);

                    return (t.EventContext, (string?)path);
                }

                return (t.EventContext, null);
            })
            .Where(t => t is (_, not null))
            .DoExpensive(async t =>
            {
                var (ctx, path) = t;
                var files = Directory.GetFiles(path!);
                var file = files[Random.Shared.Next(files.Length)];
                _context.Logger.LogInformation("Sending seiyuu image from {File}", Path.GetFullPath(file));
                return await ctx.Event.NewMessageRequest([
                    new ReplyData(ctx.Event.MessageId),
                    new ImageData($"base64://{Convert.ToBase64String(
                        await File.ReadAllBytesAsync(file, ctx.Token)
                    )}")
                ]).SendAsync(_context, ctx.Token) is { Success: true };
            }, t => t.EventContext, _context);
    }
}

// DB
public partial class SeiyuuFunction
{
    private readonly SeiyuuDbContext _db = new(context.BotContext.Uin);
    private readonly SemaphoreSlim _dbSemaphore = new(1, 1);

    private Task<bool> CreateTableAsync(CancellationToken token) =>
        _dbSemaphore.ConsumeAsync(() => _db.Database.EnsureCreatedAsync(token), token);

    private Task<int> AddAliasAsync(SeiyuuAlias alias, CancellationToken token) =>
        _dbSemaphore.ConsumeAsync(() =>
        {
            _db.Aliases.Add(alias);
            return _db.SaveChangesAsync(token);
        }, token);

    private Task<SeiyuuAlias?> GetAliasAsync(string from, CancellationToken token) =>
        _dbSemaphore.ConsumeAsync(() =>
            _db.Aliases.AsNoTracking().SingleOrDefaultAsync(a => a.From == from, token), token);
}