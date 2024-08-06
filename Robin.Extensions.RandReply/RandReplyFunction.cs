using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Robin.Abstractions;
using Robin.Abstractions.Context;
using Robin.Abstractions.Event;
using Robin.Abstractions.Event.Message;
using Robin.Abstractions.Message;
using Robin.Abstractions.Message.Entity;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Requests;
using Robin.Annotations.Filters;
using Robin.Annotations.Filters.Message;

namespace Robin.Extensions.RandReply;

[BotFunctionInfo("rand_reply", "随机回复")]
[OnAtSelf, Fallback]
// ReSharper disable once UnusedType.Global
public partial class RandReplyFunction(FunctionContext context) : BotFunction(context), IFilterHandler
{
    private RandReplyOption? _option;

    public async Task<bool> OnFilteredEventAsync(int filterGroup, long selfId, BotEvent @event, CancellationToken token)
    {
        if (@event is not GroupMessageEvent e) return false;

        var textCount = _option!.Texts?.Count ?? 0;
        var imageCount = _option.ImagePaths?.Count ?? 0;
        var index = Random.Shared.Next(textCount + imageCount);

        MessageChain chain =
        [
            new ReplyData(e.MessageId),
            index < textCount
                ? new TextData(_option.Texts![index])
                : new ImageData(
                    $"base64://{Convert.ToBase64String(await File.ReadAllBytesAsync(_option.ImagePaths![index - textCount], token))}")
        ];

        if (await new SendGroupMessageRequest(e.GroupId, chain)
            .SendAsync(_context.OperationProvider, token) is not { Success: true })
        {
            LogSendFailed(_context.Logger, e.GroupId);
            return true;
        }

        LogReplySent(_context.Logger, e.GroupId);
        return true;
    }

    public override Task StartAsync(CancellationToken token)
    {
        if (_context.Configuration.Get<RandReplyOption>() is not { } option)
        {
            LogOptionBindingFailed(_context.Logger);
            return Task.CompletedTask;
        }

        _option = option;
        return Task.CompletedTask;
    }

    #region Log

    [LoggerMessage(EventId = 0, Level = LogLevel.Error, Message = "Failed to bind option.")]
    private static partial void LogOptionBindingFailed(ILogger logger);

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Send message failed for group {GroupId}")]
    private static partial void LogSendFailed(ILogger logger, long groupId);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Reply sent for group {GroupId}")]
    private static partial void LogReplySent(ILogger logger, long groupId);

    #endregion
}