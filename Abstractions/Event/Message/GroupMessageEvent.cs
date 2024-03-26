using Robin.Abstractions.Message;
using Robin.Abstractions.Operation.Entities;

namespace Robin.Abstractions.Event.Message;

public record GroupMessageEvent(
    long Time,
    int MessageId,
    long GroupId,
    long UserId,
    AnonymousInfo Anonymous,
    MessageChain Message,
    int Font,
    GroupMessageSender Sender
) : MessageEvent(Time, MessageId, UserId, Message, Font);