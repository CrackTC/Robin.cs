using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Robin.Abstractions;
using Robin.Abstractions.Communication;
using Robin.Abstractions.Event;
using Robin.Abstractions.Event.Message;
using Robin.Abstractions.Message;
using Robin.Abstractions.Message.Entities;
using Robin.Abstractions.Operation.Requests;
using Robin.Abstractions.Operation.Responses;

namespace Robin.Extensions.ReplyAction;

[BotFunctionInfo("reply_action", "send actions on reply", typeof(GroupMessageEvent))]
// ReSharper disable once UnusedType.Global
public partial class ReplyActionFunction(
    IServiceProvider service,
    long uin,
    IOperationProvider operation,
    IConfiguration configuration,
    IEnumerable<BotFunction> functions) : BotFunction(service, uin, operation, configuration, functions)
{
    private readonly ILogger<ReplyActionFunction> _logger = service.GetRequiredService<ILogger<ReplyActionFunction>>();

    public override async Task OnEventAsync(long selfId, BotEvent @event, CancellationToken token)
    {
        if (@event is not GroupMessageEvent e) return;

        var segments = e.Message;

        var text = string.Join(' ', segments.OfType<TextData>().Select(segment => segment.Text.Trim()));

        if (!text.StartsWith('/')) return;

        var parts = text[1..].Split(' ');
        if (parts.Length == 0) return;

        if (segments.FirstOrDefault(segment => segment is ReplyData) is not ReplyData reply) return;

        if (await _operation.SendRequestAsync(new GetMessageRequest(reply.Id), token) is not GetMessageResponse
            {
                Success: true, Message: not null
            } originalMessage)
        {
            LogGetMessageFailed(_logger, reply.Id);
            return;
        }

        var senderId = originalMessage.Message.Sender.UserId;

        if (await _operation.SendRequestAsync(new GetGroupMemberInfoRequest(e.GroupId, senderId, true), token) is not
            GetGroupMemberInfoResponse { Success: true, Info: not null } info)
        {
            LogGetGroupMemberInfoFailed(_logger, e.GroupId, senderId);
            return;
        }

        var sourceName = e.Sender.Card ?? e.Sender.Nickname;
        var targetName = info.Info.Card ?? info.Info.Nickname;

        MessageChain chain =
        [
            new TextData(
                $"{sourceName} {parts[0]} {targetName}{(parts.Length > 1 ? " " + string.Join(' ', parts[1..]) : string.Empty)}")
        ];

        if (await _operation.SendRequestAsync(new SendGroupMessageRequest(e.GroupId, chain), token) is not
            { Success: true })
        {
            LogSendFailed(_logger, e.GroupId);
            return;
        }

        LogActionSent(_logger, e.GroupId);
    }

    public override Task StartAsync(CancellationToken token) => Task.CompletedTask;

    public override Task StopAsync(CancellationToken token) => Task.CompletedTask;

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