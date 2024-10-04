namespace Robin.Abstractions.Event.Meta;

[EventDescription("上游元消息")]
public abstract record MetaEvent(long Time) : BotEvent(Time);
