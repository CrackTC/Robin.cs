using Robin.Abstractions;
using Robin.Abstractions.Context;
using Robin.Abstractions.Event.Message;
using Robin.Abstractions.Message.Entity;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Requests;
using Robin.Middlewares.Fluent;
using Robin.Middlewares.Fluent.Event;
using SauceNET;

namespace Robin.Extensions.SauceNao;

[BotFunctionInfo("sauce_nao", "Saucenao 插画反向搜索")]
public partial class SauceNaoFunction(
    FunctionContext<SauceNaoOption> context
) : BotFunction<SauceNaoOption>(context), IFluentFunction
{
    private SauceNETClient? _client;

    private async Task<bool> HandleEvent(MessageEvent e, string msgId, CancellationToken token)
    {
        if (await new GetMessage(msgId).SendAsync(_context, token)
            is not { Message.Message: { } origMsg })
            return false;

        if (origMsg.OfType<ImageData>().FirstOrDefault() is not { Url: { } url })
            return false;

        var results = (await _client!.GetSauceAsync(url)).Results
            .Where(result => double.TryParse(result.Similarity, out var s) && s >= 70.0)
            .Take(3)
            .Select(result =>
                $"""
                标题: {result.Name}
                链接: {result.SourceURL}
                相似度: {result.Similarity}%
                来源: {result.DatabaseName}
                """
            )
            .ToList();

        if (results is [])
        {
            await e.NewMessageRequest([new TextData("找不到喵>_<")]).SendAsync(_context, token);
            return false;
        }

        await e.NewMessageRequest([new TextData(string.Join('\n', results))]).SendAsync(_context, token);
        return true;
    }

    public Task OnCreatingAsync(FunctionBuilder builder, CancellationToken _)
    {
        _client = new SauceNETClient(_context.Configuration.ApiKey);

        builder.On<GroupMessageEvent>("group sauce")
            .OnCommand("搜图")
            .OnReply()
            .DoExpensive(
                t => HandleEvent(t.EventContext.Event, t.MessageId, t.EventContext.Token),
                t => t.EventContext,
                _context
            )
            .On<PrivateMessageEvent>("private sauce")
            .OnCommand("搜图")
            .OnReply()
            .Do(t => HandleEvent(t.EventContext.Event, t.MessageId, t.EventContext.Token));

        return Task.CompletedTask;
    }
}
