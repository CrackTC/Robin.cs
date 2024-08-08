using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Robin.Abstractions;
using Robin.Abstractions.Context;
using Robin.Abstractions.Event.Message;
using Robin.Abstractions.Message.Entity;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Requests;
using Robin.Abstractions.Operation.Responses;
using Robin.Fluent;
using Robin.Fluent.Builder;
using SauceNET;

namespace Robin.Extensions.SauceNao;

// ReSharper disable once UnusedType.Global
[BotFunctionInfo("sauce_nao", "Saucenao 插画反向搜索")]
public partial class SauceNaoFunction(FunctionContext context) : BotFunction(context), IFluentFunction
{
    private SauceNETClient? _client;

    public string? Description { get; set; }

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
                    .SendAsync(_context.OperationProvider, token)
                    is not GetMessageResponse { Success: true, Message: { } origMsg })
                {
                    LogGetMessageFailed(_context.Logger, msgId);
                    return;
                }

                if (origMsg.Message.OfType<ImageData>().FirstOrDefault() is not { Url: { } url })
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
                    if (await e.NewMessageRequest([
                            new TextData("找不到喵>_<")
                        ]).SendAsync(_context.OperationProvider, token) is not { Success: true })
                    {
                        LogSendMessageFailed(_context.Logger, e.SourceId);
                        return;
                    }

                    LogMessageSent(_context.Logger, e.SourceId);
                    return;
                }

                if (await e.NewMessageRequest([
                        new TextData(string.Join("\n", results))
                    ]).SendAsync(_context.OperationProvider, token) is not { Success: true })
                {
                    LogSendMessageFailed(_context.Logger, e.SourceId);
                    return;
                }

                LogMessageSent(_context.Logger, e.SourceId);
            });

        return Task.CompletedTask;
    }

    #region Log

    [LoggerMessage(EventId = 0, Level = LogLevel.Warning, Message = "Failed to get message {Id}")]
    private static partial void LogGetMessageFailed(ILogger logger, string id);

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Option binding failed")]
    private static partial void LogOptionBindingFailed(ILogger logger);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Failed to send message to {GroupId}")]
    private static partial void LogSendMessageFailed(ILogger logger, long groupId);

    [LoggerMessage(EventId = 4, Level = LogLevel.Information, Message = "Message sent to {GroupId}")]
    private static partial void LogMessageSent(ILogger logger, long groupId);

    #endregion
}