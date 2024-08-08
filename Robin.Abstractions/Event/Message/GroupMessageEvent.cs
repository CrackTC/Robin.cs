using Robin.Abstractions.Entity;
using Robin.Abstractions.Message;
using Robin.Abstractions.Operation.Requests;

namespace Robin.Abstractions.Event.Message;

[EventDescription("群聊消息")]
public record GroupMessageEvent(
    long Time,
    string MessageId,
    long GroupId,
    long UserId,
    AnonymousInfo? Anonymous,
    MessageChain Message,
    int Font,
    GroupMessageSender Sender
) : MessageEvent(Time, MessageId, UserId, Message, Font)
{
    public override long SourceId => GroupId;

    public override Operation.Request NewMessageRequest(MessageChain chain) =>
        new SendGroupMessageRequest(SourceId, chain);
};