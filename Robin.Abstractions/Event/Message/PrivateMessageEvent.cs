using Robin.Abstractions.Entities;
using Robin.Abstractions.Message;

namespace Robin.Abstractions.Event.Message;

public record PrivateMessageEvent(
    long Time,
    string MessageId,
    long UserId,
    MessageChain Message,
    int Font,
    MessageSender Sender
) : MessageEvent(Time, MessageId, UserId, Message, Font);