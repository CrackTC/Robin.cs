using Robin.Abstractions.Message;

namespace Robin.Abstractions.Event.Message;

[EventDescription("消息")]
public abstract record MessageEvent(
    long Time,
    string MessageId,
    long UserId,
    MessageChain Message,
    int Font
) : BotEvent(Time)
{
    public abstract long SourceId { get; }
};
