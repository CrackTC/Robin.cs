using System.Text.RegularExpressions;
using Robin.Abstractions;
using Robin.Abstractions.Context;
using Robin.Abstractions.Event.Message;
using Robin.Abstractions.Message.Entity;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Requests;
using Robin.Middlewares.Fluent;
using Robin.Middlewares.Fluent.Event;

namespace Robin.Extensions.Jm;

[BotFunctionInfo("jm", "JM")]
public partial class JmFunction(
    FunctionContext<JmOption> context
) : BotFunction<JmOption>(context), IFluentFunction
{
    [GeneratedRegex(@"^/jm\s*(?<id>\d+)(?:\s+(?<index>\d+))?$")]
    private static partial Regex JmRegex { get; }

    private static readonly HttpClient _client = new() { Timeout = TimeSpan.FromMinutes(10) };

    private async Task SendErrorAsync(EventContext<GroupMessageEvent> ctx, string message) =>
        await ((Task)ctx.Event.NewMessageRequest([
            new ReplyData(ctx.Event.MessageId),
            new TextData(message)
        ]).SendAsync(_context, ctx.Token)).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

    public Task OnCreatingAsync(FunctionBuilder builder, CancellationToken _)
    {
        builder.On<GroupMessageEvent>()
            .OnRegex(JmRegex)
            .DoExpensive(async t =>
            {
                var (ctx, match) = t;
                if (!int.TryParse(match.Groups["id"].Value, out var id))
                {
                    await SendErrorAsync(ctx, "ID不合法喵");
                    return false;
                }

                int? index = null;
                if (match.Groups["index"].Success)
                {
                    if (!int.TryParse(match.Groups["index"].Value, out var parsedIndex))
                    {
                        await SendErrorAsync(ctx, "章节号不合法喵");
                        return false;
                    }

                    index = parsedIndex;
                }

                var fileName = Path.Combine("jm", $"jm_{id}_{index.GetValueOrDefault(1)}.pdf");
                if (File.Exists(fileName))
                    return await new UploadGroupFileRequest(ctx.Event.GroupId, fileName).SendAsync(_context, ctx.Token) is { Success: true }
                        && await ctx.Event.NewMessageRequest([
                            new ReplyData(ctx.Event.MessageId),
                            new ImageData($"{_context.Configuration.ApiAddress}/preview/{id}/{index.GetValueOrDefault(1)}/00001.webp")
                        ]).SendAsync(_context, ctx.Token) is { Success: true };

                int photoCount;
                try
                {
                    photoCount = int.Parse(await _client.GetStringAsync($"{_context.Configuration.ApiAddress}/count?id={id}", ctx.Token));
                }
                catch (Exception)
                {
                    await SendErrorAsync(ctx, "获取章节数失败>_<");
                    return false;
                }

                if (photoCount is -1)
                {
                    await SendErrorAsync(ctx, "没有这本喵");
                    return false;
                }

                if (photoCount is > 1)
                {
                    if (index is null)
                    {
                        await SendErrorAsync(ctx, $"存在多个章节，请指定章节号喵[1-{photoCount}]: /jm <id> <章节号>");
                        return false;
                    }

                    if (index is < 1 || index > photoCount)
                    {
                        await SendErrorAsync(ctx, $"没有那一章喵[1-{photoCount}]");
                        return false;
                    }
                }

                if (!Directory.Exists("jm")) Directory.CreateDirectory("jm");

                var resp = await _client.GetStringAsync($"{_context.Configuration.ApiAddress}/download?id={id}&index={index.GetValueOrDefault(1)}", ctx.Token);
                if (resp.Split(',') is not [var location, var preview])
                {
                    await SendErrorAsync(ctx, "下载失败>_<");
                    return false;
                }

                await using (var targetStream = File.Create(fileName))
                await using (var stream = await _client.GetStreamAsync($"{_context.Configuration.ApiAddress}{location}", ctx.Token))
                    await stream.CopyToAsync(targetStream, ctx.Token);

                return await new UploadGroupFileRequest(ctx.Event.GroupId, fileName).SendAsync(_context, ctx.Token) is { Success: true }
                    && await ctx.Event.NewMessageRequest([
                        new ReplyData(ctx.Event.MessageId),
                        new ImageData($"{_context.Configuration.ApiAddress}{preview}")
                    ]).SendAsync(_context, ctx.Token) is { Success: true };
            }, t => t.EventContext, _context);

        return Task.CompletedTask;
    }
}
