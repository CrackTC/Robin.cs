namespace Robin.Abstractions.Event.Request;

[EventDescription("请求事件")]
public abstract record RequestEvent(
    long Time,
    long UserId,
    string Comment,
    string Flag
) : BotEvent(Time);
