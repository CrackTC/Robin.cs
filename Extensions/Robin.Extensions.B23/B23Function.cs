using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Robin.Abstractions;
using Robin.Abstractions.Context;
using Robin.Abstractions.Event.Message;
using Robin.Abstractions.Event.Notice.Recall;
using Robin.Abstractions.Message.Entity;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Requests;
using Robin.Middlewares.Fluent;
using Robin.Middlewares.Fluent.Event;

namespace Robin.Extensions.B23;

[BotFunctionInfo("b23", "不要b23")]
public partial class B23Function(FunctionContext context) : BotFunction(context), IFluentFunction
{
    private static readonly HttpClient _client = new(
        new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false }
    );

    [GeneratedRegex(@"^https://www\.bilibili\.com/video/[^?]+")]
    private partial Regex RawUrlRegex { get; }

    private async Task<string?> ResolveB23(string b23Url, CancellationToken token)
    {
        using var resp = await _client.GetAsync(b23Url, token);

        if (resp is not { StatusCode: HttpStatusCode.Found, Headers.Location: { } location })
            return null;
        LogB23Redirect(_context.Logger, location);
        return RawUrlRegex.Match(location.ToString()) is { Success: true, Value: var value }
            ? value
            : null;
    }

    public Task OnCreatingAsync(FunctionBuilder builder, CancellationToken _)
    {
        Dictionary<string, string> conversion = [];
        LinkedList<(string orig, DateTime expire)> expiration = [];

        // cat card.json | jq .Message[0].Content --raw-output | jq .meta.detail_1.qqdocurl
        builder
            .On<MessageEvent>()
            .OnJson()
            .Select(t =>
                (
                    ctx: t.EventContext,
                    url: t.Json?["meta"]?["detail_1"]?["qqdocurl"]?.GetValue<string>()
                        ?? t.Json?["meta"]?["news"]?["jumpUrl"]?.GetValue<string>()
                        ?? string.Empty
                )
            )
            .Where(t => t.url.StartsWith("https://b23.tv/"))
            .Do(async t =>
            {
                var (ctx, url) = t;

                LogExtractB23(_context.Logger, url);

                if (await ResolveB23(url, ctx.Token) is not { } resolved)
                    return;
                LogB23Resolved(_context.Logger, resolved);

                if (
                    await ctx
                        .Event.NewMessageRequest([
                            new ReplyData(ctx.Event.MessageId),
                            new TextData(resolved),
                        ])
                        .SendAsync(_context, ctx.Token)
                    is not { MessageId: { } id }
                )
                    return;

                conversion.Add(ctx.Event.MessageId, id);
                expiration.AddLast((ctx.Event.MessageId, DateTime.Now.AddMinutes(5)));

                while (expiration.First is { Value: var (orig, expire) } && expire < DateTime.Now)
                {
                    conversion.Remove(orig);
                    expiration.RemoveFirst();
                }
            })
            .On<RecallEvent>()
            .Where(ctx => conversion.ContainsKey(ctx.Event.MessageId))
            .Do(ctx =>
                new RecallMessage(conversion[ctx.Event.MessageId]).SendAsync(_context, ctx.Token)
            );

        return Task.CompletedTask;
    }

    #region Log

    [LoggerMessage(Level = LogLevel.Information, Message = "Extract B23: {url}")]
    private static partial void LogExtractB23(ILogger logger, string url);

    [LoggerMessage(Level = LogLevel.Information, Message = "B23 redirect to {url}")]
    private static partial void LogB23Redirect(ILogger logger, Uri url);

    [LoggerMessage(Level = LogLevel.Information, Message = "B23 resolved to {url}")]
    private static partial void LogB23Resolved(ILogger logger, string url);

    #endregion
}
