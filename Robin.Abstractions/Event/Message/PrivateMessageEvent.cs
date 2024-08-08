using Robin.Abstractions.Entity;
using Robin.Abstractions.Message;
using Robin.Abstractions.Operation.Requests;

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

    public override Operation.Request NewMessageRequest(MessageChain chain) =>
        new SendPrivateMessageRequest(SourceId, chain);
};