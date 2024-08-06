using Microsoft.Extensions.Logging;
using Robin.Abstractions;
using Robin.Abstractions.Context;
using Robin.Abstractions.Event;
using Robin.Abstractions.Event.Message;
using Robin.Abstractions.Message;
using Robin.Abstractions.Message.Entity;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Requests;
using Robin.Abstractions.Operation.Responses;
using Robin.Annotations.Filters;
using Robin.Annotations.Filters.Message;

namespace Robin.Extensions.ReplyAction;

[BotFunctionInfo("reply_action", "把字句制造机")]
[OnReply]
// ReSharper disable once UnusedType.Global
public partial class ReplyActionFunction(FunctionContext context) : BotFunction(context), IFilterHandler
{
    public async Task<bool> OnFilteredEventAsync(int filterGroup, long selfId, BotEvent @event, CancellationToken token)
    {
        if (@event is not GroupMessageEvent e) return false;

        var text = string.Join(' ', e.Message
            .OfType<TextData>()
            .Select(segment => segment.Text.Trim())
            .Where(text => !string.IsNullOrEmpty(text)));

        if (!text.StartsWith('/')) return false;

        var parts = text[1..].Split(' ');
        if (parts.Length == 0) return false;

        var reply = e.Message.OfType<ReplyData>().First();

        if (await new GetMessageRequest(reply.Id).SendAsync(_context.OperationProvider, token)
            is not GetMessageResponse { Success: true, Message: not null } originalMessage)
        {
            LogGetMessageFailed(_context.Logger, reply.Id);
            return true;
        }

        var senderId = originalMessage.Message.Sender.UserId;

        if (await new GetGroupMemberInfoRequest(e.GroupId, senderId, true).SendAsync(_context.OperationProvider, token)
            is not GetGroupMemberInfoResponse { Success: true, Info: not null } info)
        {
            LogGetGroupMemberInfoFailed(_context.Logger, e.GroupId, senderId);
            return true;
        }

        var sourceName = string.IsNullOrEmpty(e.Sender.Card) ? e.Sender.Nickname : e.Sender.Card;
        var targetName = string.IsNullOrEmpty(info.Info.Card) ? info.Info.Nickname : info.Info.Card;

        MessageChain chain =
        [
            new TextData($"{sourceName} {parts[0]} {targetName}{(parts.Length > 1 ? " " + string.Join(' ', parts[1..]) : string.Empty)}")
        ];

        if (await new SendGroupMessageRequest(e.GroupId, chain)
            .SendAsync(_context.OperationProvider, token) is not { Success: true })
        {
            LogSendFailed(_context.Logger, e.GroupId);
            return true;
        }

        LogActionSent(_context.Logger, e.GroupId);
        return true;
    }

    #region Log

    [LoggerMessage(EventId = 0, Level = LogLevel.Warning, Message = "Failed to get message {Id}")]
    private static partial void LogGetMessageFailed(ILogger logger, string id);

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning,
        Message = "Failed to get group member info {GroupId} {UserId}")]
    private static partial void LogGetGroupMemberInfoFailed(ILogger logger, long groupId, long userId);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Send message failed for group {GroupId}")]
    private static partial void LogSendFailed(ILogger logger, long groupId);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "Action sent for group {GroupId}")]
    private static partial void LogActionSent(ILogger logger, long groupId);

    #endregion
}