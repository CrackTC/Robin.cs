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

namespace Robin.Extensions.Gray;

[BotFunctionInfo("gray", "喜多烧香精神续作（x")]
// ReSharper disable once UnusedType.Global
public partial class GrayFunction(FunctionContext context) : BotFunction(context), IFluentFunction
{
    private GrayOption? _option;

    public string? Description { get; set; }

    public Task OnCreatingAsync(FunctionBuilder builder, CancellationToken token)
    {
        if (_context.Configuration.Get<GrayOption>() is not { } option)
        {
            LogOptionBindingFailed(_context.Logger);
            return Task.CompletedTask;
        }

        _option = option;

        builder.On<GroupMessageEvent>()
            .OnCommand("送走")
            .OnReply()
            .Do(async t =>
            {
                var (ctx, msgId) = t;
                if (await new GetMessageRequest(msgId)
                    .SendAsync(_context.OperationProvider, ctx.Token)
                    is not GetMessageResponse { Success: true, Message: { } origMsg })
                {
                    LogGetMessageFailed(_context.Logger, msgId);
                    return;
                }

                var senderId = origMsg.Sender.UserId;

                try
                {
                    var url = $"{_option.ApiAddress}/?id={senderId}";
                    if (await ctx.Event.NewMessageRequest([
                            new ImageData(url)
                        ]).SendAsync(_context.OperationProvider, ctx.Token) is not { Success: true })
                    {
                        LogSendFailed(_context.Logger, ctx.Event.GroupId);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    LogGetImageFailed(_context.Logger, senderId, ex);
                    return;
                }

                LogImageSent(_context.Logger, ctx.Event.GroupId);
            });

        return Task.CompletedTask;
    }

    #region Log

    [LoggerMessage(EventId = 0, Level = LogLevel.Error, Message = "Failed to bind option.")]
    private static partial void LogOptionBindingFailed(ILogger logger);

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Send message failed for group {GroupId}")]
    private static partial void LogSendFailed(ILogger logger, long groupId);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Image sent for group {GroupId}")]
    private static partial void LogImageSent(ILogger logger, long groupId);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "Failed to get message {Id}")]
    private static partial void LogGetMessageFailed(ILogger logger, string id);

    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Failed to get image {Id}")]
    private static partial void LogGetImageFailed(ILogger logger, long id, Exception ex);

    #endregion
}