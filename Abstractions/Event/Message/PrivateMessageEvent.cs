using Robin.Abstractions.Message;
using Robin.Abstractions.Operation.Entities;

namespace Robin.Abstractions.Event.Message;

public record PrivateMessageEvent(
    long Time,
    int MessageId,
    long UserId,
    MessageChain Message,
    int Font,
    MessageSender Sender
) : MessageEvent(Time, MessageId, UserId, Message, Font);