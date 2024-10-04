namespace Robin.Abstractions.Event.Notice;

[EventDescription("通知事件")]
public abstract record NoticeEvent(long Time) : BotEvent(Time);
