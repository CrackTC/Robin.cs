using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Robin.Abstractions;
using Robin.Abstractions.Context;
using Robin.Abstractions.Event;
using Robin.Abstractions.Event.Message;
using Robin.Abstractions.Message.Entity;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Requests;
using Robin.Abstractions.Operation.Responses;
using Robin.Annotations.Filters;
using Robin.Annotations.Filters.Message;
using SauceNET;

namespace Robin.Extensions.SauceNao;

// ReSharper disable once UnusedType.Global
[BotFunctionInfo("sauce_nao", "Saucenao 插画反向搜索")]
[OnReply, OnCommand("搜图")]
public partial class SauceNaoFunction(FunctionContext context) : BotFunction(context), IFilterHandler
{
    private SauceNETClient? _client;

    public override Task StartAsync(CancellationToken token)
    {
        if (_context.Configuration.Get<SauceNaoOption>() is not { } option)
        {
            LogOptionBindingFailed(_context.Logger);
            return Task.CompletedTask;
        }

        _client = new SauceNETClient(option.ApiKey);

        return Task.CompletedTask;
    }

    public async Task<bool> OnFilteredEventAsync(int filterGroup, EventContext eventContext)
    {
        if (eventContext.Event is not GroupMessageEvent e) return false;

        var reply = e.Message.OfType<ReplyData>().First();

        if (await new GetMessageRequest(reply.Id).SendAsync(_context.OperationProvider, eventContext.Token)
            is not GetMessageResponse { Success: true, Message: not null } originalMessage)
        {
            LogGetMessageFailed(_context.Logger, reply.Id);
            return true;
        }

        if (originalMessage.Message.Message.FirstOrDefault(segment => segment is ImageData) is not ImageData image)
            return true;

        var sauce = await _client!.GetSauceAsync(image.Url);
        var results = sauce.Results
            .Where(result => double.TryParse(result.Similarity, out var s) && s >= 70.0)
            .Take(3)
            .Select(result =>
                string.Join(
                    '\n',
                    $"标题: {result.Name}",
                    $"链接: {result.SourceURL}",
                    $"相似度: {result.Similarity}%",
                    $"来源: {result.DatabaseName}"
                )
            ).ToList();

        if (results.Count == 0)
        {
            if (await new SendGroupMessageRequest(e.GroupId, [
                    new TextData("找不到喵>_<")
                ]).SendAsync(_context.OperationProvider, eventContext.Token) is not { Success: true })
            {
                LogSendMessageFailed(_context.Logger, e.GroupId);
                return true;
            }

            LogMessageSent(_context.Logger, e.GroupId);
            return true;
        }

        if (await new SendGroupMessageRequest(e.GroupId, [
                new TextData(string.Join("\n", results))
            ]).SendAsync(_context.OperationProvider, eventContext.Token) is not { Success: true })
        {
            LogSendMessageFailed(_context.Logger, e.GroupId);
            return true;
        }

        LogMessageSent(_context.Logger, e.GroupId);
        return true;
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