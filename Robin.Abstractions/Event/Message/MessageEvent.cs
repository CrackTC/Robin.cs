using Robin.Abstractions.Message;

namespace Robin.Abstractions.Event.Message;

public abstract record MessageEvent(
    long Time,
    string MessageId,
    long UserId,
    MessageChain Message,
    int Font
) : BotEvent(Time);