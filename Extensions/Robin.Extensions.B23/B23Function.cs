using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Robin.Abstractions;
using Robin.Abstractions.Context;
using Robin.Abstractions.Event.Message;
using Robin.Abstractions.Message.Entity;
using Robin.Abstractions.Operation;
using Robin.Middlewares.Fluent;
using Robin.Middlewares.Fluent.Event;

namespace Robin.Extensions.B23;

[BotFunctionInfo("b23", "不要b23")]
public partial class B23Function(FunctionContext context) : BotFunction(context), IFluentFunction
{
    private static readonly HttpClient _client = new(new HttpClientHandler
    {
        AllowAutoRedirect = false,
        UseCookies = false,
    });

    [GeneratedRegex(@"^https://www\.bilibili\.com/video/[^?]+")]
    private partial Regex RawUrlRegex { get; }

    private async Task<string?> ResolveB23(string b23Url, CancellationToken token)
    {
        using var resp = await _client.GetAsync(b23Url, token);

        if (resp is not { StatusCode: HttpStatusCode.Found, Headers.Location: { } location }) return null;
        LogB23Redirect(_context.Logger, location.ToString());
        return RawUrlRegex.Match(location.ToString()) is { Success: true, Value: var value } ? value : null;
    }

    public Task OnCreatingAsync(FunctionBuilder builder, CancellationToken _)
    {
        // cat card.json | jq .Message[0].Content --raw-output | jq .meta.detail_1.qqdocurl
        builder.On<MessageEvent>()
            .OnJson()
            .Do(async t =>
            {
                var (ctx, json) = t;
                var url = json?["meta"]?["detail_1"]?["qqdocurl"]?.GetValue<string>()
                    ?? json?["meta"]?["news"]?["jumpUrl"]?.GetValue<string>()
                    ?? string.Empty;

                if (!url.StartsWith("https://b23.tv/")) return;
                LogExtractB23(_context.Logger, url);

                if (await ResolveB23(url, ctx.Token) is not { } resolved) return;

                LogB23Resolved(_context.Logger, resolved);
                await ctx.Event.NewMessageRequest([
                    new ReplyData(ctx.Event.MessageId),
                    new TextData(resolved)
                ]).SendAsync(_context, ctx.Token);
            });

        return Task.CompletedTask;
    }

    #region Log

    [LoggerMessage(Level = LogLevel.Information, Message = "Extract B23: {url}")]
    private static partial void LogExtractB23(ILogger logger, string url);

    [LoggerMessage(Level = LogLevel.Information, Message = "B23 redirect to {url}")]
    private static partial void LogB23Redirect(ILogger logger, string url);

    [LoggerMessage(Level = LogLevel.Information, Message = "B23 resolved to {url}")]
    private static partial void LogB23Resolved(ILogger logger, string url);

    #endregion
}
