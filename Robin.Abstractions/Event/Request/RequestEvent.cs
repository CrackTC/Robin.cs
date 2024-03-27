namespace Robin.Abstractions.Event.Request;

public abstract record RequestEvent(
    long Time,
    long UserId,
    string Comment,
    string Flag
) : BotEvent(Time);