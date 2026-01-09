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

namespace Robin.Extensions.Seiyuu;

[BotFunctionInfo("seiyuu", "声优图片")]
public partial class SeiyuuFunction(
    FunctionContext<SeiyuuOption> context
) : BotFunction<SeiyuuOption>(context), IFluentFunction
{
    [GeneratedRegex(@"^/加图\s*(?<name>\S+)$")]
    private static partial Regex AddImageRegex { get; }

    [GeneratedRegex(@"^/添加别名\s*(?<from>\S+)\s*(?<to>\S+)$")]
    private static partial Regex AddAliasRegex { get; }

    private static readonly HttpClient _client = new() { Timeout = TimeSpan.FromMinutes(10) };

    private async Task SendErrorAsync(EventContext<MessageEvent> ctx, string message) =>
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

    public Task OnCreatingAsync(FunctionBuilder builder, CancellationToken _)
    {
        builder.On<MessageEvent>()
            .Where(t => !_context.Configuration.BannedIds.Contains(t.Event.UserId))
            .OnRegex(AddAliasRegex)
            .DoExpensive(async t =>
            {
                var (ctx, match) = t;
                var from = Path.Join("seiyuu", EscapeName(match.Groups["from"].Value));
                var to = Path.Join("seiyuu", EscapeName(match.Groups["to"].Value));

                if (Directory.Exists(from))
                {
                    await SendErrorAsync(ctx, $"已经存在文件夹：{from}");
                    return false;
                }

                if (!Directory.Exists(to))
                {
                    await SendErrorAsync(ctx, $"不存在文件夹：{to}");
                    return false;
                }

                Directory.CreateSymbolicLink(from, EscapeName(match.Groups["to"].Value));
                await SendErrorAsync(ctx, "添加成功");
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
                    await SendErrorAsync(ctx, "需要引用一条包含图片的消息哦");
                    return false;
                }

                var imgs = msg.OfType<ImageData>()
                    .Where(img => img.Url is not null).ToList();
                if (imgs is [])
                {
                    await SendErrorAsync(ctx, "需要引用一条包含图片的消息哦");
                    return false;
                }

                var directory = Path.Join("seiyuu", name);
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
                        await SendErrorAsync(ctx, $"保存图片{i + 1}失败：{e.Message}");
                        continue;
                    }

                    ++successCount;
                }

                if (successCount == imgs.Count)
                    await SendErrorAsync(ctx, "加图成功");

                return successCount == imgs.Count;
            }, t => t.EventContext, _context)
            .On<MessageEvent>()
            .Where(t => !_context.Configuration.BannedIds.Contains(t.Event.UserId))
            .AsFallback()
            .Where(ctx => ctx.Event.Message.All(data => data is TextData))
            .OnText()
            .Where(t =>
            {
                var name = EscapeName(t.Text);
                if (string.IsNullOrEmpty(name)) return false;
                if (_context.Configuration.GroupLimits.ContainsKey(name))
                {
                    var limits = _context.Configuration.GroupLimits[name];
                    if (!limits.Contains(t.EventContext.Event.SourceId)) return false;
                }
                var path = Path.Join("seiyuu", name);
                return Directory.Exists(path) && Directory.EnumerateFiles(path).Any();
            })
            .DoExpensive(async t =>
            {
                var dir = Path.Join("seiyuu", EscapeName(t.Text));
                var files = Directory.GetFiles(dir);
                var file = files[Random.Shared.Next(files.Length)];
                _context.Logger.LogInformation("Sending seiyuu image from {File}", Path.GetFullPath(file));
                return await t.EventContext.Event.NewMessageRequest([
                    new ReplyData(t.EventContext.Event.MessageId),
                    new ImageData($"base64://{Convert.ToBase64String(await File.ReadAllBytesAsync(
                        file,
                        t.EventContext.Token
                    ))}")
                ]).SendAsync(_context, t.EventContext.Token) is { Success: true };
            }, t => t.EventContext, _context);
        return Task.CompletedTask;
    }
}
