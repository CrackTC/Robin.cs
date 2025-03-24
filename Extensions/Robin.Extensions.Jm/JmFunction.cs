using System.Text.RegularExpressions;
using Robin.Abstractions;
using Robin.Abstractions.Context;
using Robin.Abstractions.Event.Message;
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
    [GeneratedRegex(@"^/jm (?<id>\d+)$")]
    private static partial Regex JmRegex { get; }

    private static readonly HttpClient _client = new();

    public Task OnCreatingAsync(FunctionBuilder builder, CancellationToken token)
    {
        builder.On<GroupMessageEvent>()
            .OnRegex(JmRegex)
            .DoExpensive(async t =>
            {
                var (ctx, match) = t;
                var id = int.Parse(match.Groups["id"].Value);

                var location = await _client.GetStringAsync($"{_context.Configuration.ApiAddress}/download?id={id}", ctx.Token);
                var fileUrl = $"{_context.Configuration.ApiAddress}{location}";

                using var resp = await _client.GetAsync(fileUrl, ctx.Token);
                if (!resp.IsSuccessStatusCode) return false;

                if (!Directory.Exists("jm")) Directory.CreateDirectory("jm");

                string fileName = Path.Combine(
                        "jm",
                        resp.Content.Headers.ContentDisposition?.FileNameStar
                            ?? resp.Content.Headers.ContentDisposition?.FileName
                            ?? $"jm_{id}.pdf");

                using (var targetStream = File.Create(fileName))
                {
                    await resp.Content.CopyToAsync(targetStream, ctx.Token);
                }

                if (await new UploadGroupFileRequest(ctx.Event.GroupId, fileName).SendAsync(_context, ctx.Token)
                    is not { Success: true })
                    return false;

                return true;
            }, t => t.EventContext, _context);

        return Task.CompletedTask;
    }
}
