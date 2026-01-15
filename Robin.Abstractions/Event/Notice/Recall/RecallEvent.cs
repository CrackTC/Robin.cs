namespace Robin.Abstractions.Event.Notice.Recall;

[EventDescription("消息撤回")]
public abstract record RecallEvent(long Time, long UserId, string MessageId) : NoticeEvent(Time);
