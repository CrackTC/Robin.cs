namespace Robin.Abstractions.Event.Notice.Recall;

public abstract record RecallEvent(
    long Time,
    long UserId,
    long MessageId
) : NoticeEvent(Time);