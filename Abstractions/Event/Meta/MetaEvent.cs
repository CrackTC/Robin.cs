namespace Robin.Abstractions.Event.Meta;

public abstract record MetaEvent(long Time) : BotEvent(Time);