using System.Net;
using System.Text.RegularExpressions;
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
    private partial Regex RawUrlRegex();

    private async Task<string?> ResolveB23(string b23Url, CancellationToken token)
    {
        using var resp = await _client.GetAsync(b23Url, token);

        if (resp is not { StatusCode: HttpStatusCode.Found, Headers.Location: { } location }) return null;
        return RawUrlRegex().Match(location.ToString())?.Value;
    }

    public Task OnCreatingAsync(FunctionBuilder builder, CancellationToken token)
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
                if (await ResolveB23(url, ctx.Token) is not { } resolved) return;

                await ctx.Event.NewMessageRequest([
                    new ReplyData(ctx.Event.MessageId),
                    new TextData(resolved)
                ]).SendAsync(_context.BotContext.OperationProvider, _context.Logger, ctx.Token);
            });

        return Task.CompletedTask;
    }
}
