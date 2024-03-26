namespace Robin.Abstractions.Event.Notice;

public abstract record NoticeEvent(long Time) : BotEvent(Time);