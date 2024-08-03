namespace Robin.Abstractions.Event;

[EventDescription("任意事件")]
public abstract record BotEvent(long Time);