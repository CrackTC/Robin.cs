using Robin.Abstractions.Entity;
using Robin.Abstractions.Message;

namespace Robin.Abstractions.Event.Message;

[EventDescription("私聊消息")]
public record PrivateMessageEvent(
    long Time,
    string MessageId,
    long UserId,
    MessageChain Message,
    int Font,
    MessageSender Sender
) : MessageEvent(Time, MessageId, UserId, Message, Font)
{
    public override long SourceId => UserId;
};
