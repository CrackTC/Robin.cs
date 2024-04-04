using Robin.Abstractions.Entities;
using Robin.Abstractions.Message;

namespace Robin.Abstractions.Event.Message;

public record GroupMessageEvent(
    long Time,
    string MessageId,
    long GroupId,
    long UserId,
    AnonymousInfo? Anonymous,
    MessageChain Message,
    int Font,
    GroupMessageSender Sender
) : MessageEvent(Time, MessageId, UserId, Message, Font);