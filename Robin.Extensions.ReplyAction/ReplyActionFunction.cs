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
using System.Text.RegularExpressions;

namespace Robin.Extensions.ReplyAction;

[BotFunctionInfo("reply_action", "把字句制造机")]
// ReSharper disable once UnusedType.Global
public partial class ReplyActionFunction(FunctionContext context) : BotFunction(context), IFluentFunction
{
    public string? Description { get; set; }

    [GeneratedRegex(@"^/\S")]
    private static partial Regex IsAction();

    [GeneratedRegex(@"^/(?<verb>\S+)(?:\s+(?<adverb>.*))?")]
    private static partial Regex ActionParts();

    public Task OnCreatingAsync(FunctionBuilder builder, CancellationToken _)
    {
        builder.On<GroupMessageEvent>()
            .OnRegex(IsAction())
            .Select(e => e.EventContext)
            .OnReply()
            .AsFallback()
            .Do(async t =>
            {
                var (ctx, msgId) = t;
                var (e, token) = ctx;

                var match = ActionParts().Match(
                    string.Join(
                        null,
                        e.Message.OfType<TextData>()
                            .Select(data => data.Text.Trim())
                    )
                );

                var verb = match.Groups["verb"];
                var adverb = match.Groups["adverb"];

                if (await new GetMessageRequest(msgId).SendAsync(_context.OperationProvider, token)
                    is not GetMessageResponse { Success: true, Message: { } origMsg })
                {
                    LogGetMessageFailed(_context.Logger, msgId);
                    return;
                }

                var senderId = origMsg.Sender.UserId;

                if (await new GetGroupMemberInfoRequest(e.GroupId, senderId, true)
                        .SendAsync(_context.OperationProvider, token)
                    is not GetGroupMemberInfoResponse { Success: true, Info: { } info })
                {
                    LogGetGroupMemberInfoFailed(_context.Logger, e.GroupId, senderId);
                    return;
                }

                var sourceName = e.Sender.Card switch
                {
                    null or "" => e.Sender.Nickname,
                    _ => e.Sender.Card
                };

                var targetName = info.Card switch
                {
                    null or "" => info.Nickname,
                    _ => info.Card
                };


                if (await e.NewMessageRequest([
                        new TextData($"{sourceName} {verb.Value} {targetName}{(
                            adverb.Success ? ' ' + adverb.Value : string.Empty
                        )}")
                    ]).SendAsync(_context.OperationProvider, token) is not { Success: true })
                {
                    LogSendFailed(_context.Logger, e.GroupId);
                    return;
                }

                LogActionSent(_context.Logger, e.GroupId);
            });

        return Task.CompletedTask;
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