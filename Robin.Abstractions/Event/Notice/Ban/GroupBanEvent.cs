namespace Robin.Abstractions.Event.Notice.Ban;

public abstract record GroupBanEvent(
    long Time,
    long GroupId,
    long OperatorId,
    long UserId,
    long Duration
) : NoticeEvent(Time);