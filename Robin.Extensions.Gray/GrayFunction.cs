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

namespace Robin.Extensions.Gray;

[BotFunctionInfo("gray", "喜多烧香精神续作（x")]
[OnReply, OnCommand("送走")]
// ReSharper disable once UnusedType.Global
public partial class GrayFunction(FunctionContext context) : BotFunction(context), IFilterHandler
{
    private GrayOption? _option;
    private static readonly HttpClient _client = new();

    public override Task StartAsync(CancellationToken token)
    {
        if (_context.Configuration.Get<GrayOption>() is not { } option)
        {
            LogOptionBindingFailed(_context.Logger);
            return Task.CompletedTask;
        }

        _option = option;
        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken token)
    {
        _client.Dispose();
        return Task.CompletedTask;
    }


    public async Task<bool> OnFilteredEventAsync(int filterGroup, EventContext eventContext)
    {
        if (eventContext.Event is not GroupMessageEvent e) return false;

        var segments = e.Message;

        var reply = segments.OfType<ReplyData>().First();

        if (await new GetMessageRequest(reply.Id)
            .SendAsync(_context.OperationProvider, eventContext.Token) is not GetMessageResponse { Success: true, Message: not null } originalMessage)
        {
            LogGetMessageFailed(_context.Logger, reply.Id);
            return true;
        }

        var senderId = originalMessage.Message.Sender.UserId;

        try
        {
            var url = $"{_option!.ApiAddress}/?id={senderId}";
            if (await new SendGroupMessageRequest(e.GroupId, [
                    new ImageData(url)
                ]).SendAsync(_context.OperationProvider, eventContext.Token) is not { Success: true })
            {
                LogSendFailed(_context.Logger, e.GroupId);
                return true;
            }
        }
        catch (Exception ex)
        {
            LogGetImageFailed(_context.Logger, senderId.ToString(), ex);
            return true;
        }

        LogImageSent(_context.Logger, e.GroupId);
        return true;
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
    private static partial void LogGetImageFailed(ILogger logger, string id, Exception ex);

    #endregion
}