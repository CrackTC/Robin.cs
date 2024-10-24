using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Robin.Abstractions;
using Robin.Abstractions.Context;
using Robin.Abstractions.Event.Message;
using Robin.Abstractions.Message.Entity;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Requests;
using Robin.Abstractions.Operation.Responses;
using Robin.Middlewares.Fluent;
using Robin.Middlewares.Fluent.Event;
using SauceNET;

namespace Robin.Extensions.SauceNao;

// ReSharper disable once UnusedType.Global
[BotFunctionInfo("sauce_nao", "Saucenao 插画反向搜索")]
public partial class SauceNaoFunction(FunctionContext context) : BotFunction(context), IFluentFunction
{
    private SauceNETClient? _client;

    public Task OnCreatingAsync(FunctionBuilder builder, CancellationToken _)
    {
        if (_context.Configuration.Get<SauceNaoOption>() is not { } option)
        {
            LogOptionBindingFailed(_context.Logger);
            return Task.CompletedTask;
        }

        _client = new SauceNETClient(option.ApiKey);

        builder.On<MessageEvent>()
            .OnCommand("搜图")
            .OnReply()
            .Do(async t =>
            {
                var (ctx, msgId) = t;
                var (e, token) = ctx;

                if (await new GetMessageRequest(msgId)
                        .SendAsync<GetMessageResponse>(_context.OperationProvider, _context.Logger, token)
                    is not { Message.Message: { } origMsg })
                    return;

                if (origMsg.OfType<ImageData>().FirstOrDefault() is not { Url: { } url })
                    return;

                var results = (await _client.GetSauceAsync(url)).Results
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

                if (results.Count is 0)
                {
                    await e.NewMessageRequest([
                        new TextData("找不到喵>_<")
                    ]).SendAsync(_context.OperationProvider, _context.Logger, token);

                    return;
                }

                await e.NewMessageRequest([
                    new TextData(string.Join("\n", results))
                ]).SendAsync(_context.OperationProvider, _context.Logger, token);
            });

        return Task.CompletedTask;
    }

    #region Log

    [LoggerMessage(Level = LogLevel.Warning, Message = "Option binding failed")]
    private static partial void LogOptionBindingFailed(ILogger logger);

    #endregion
}
