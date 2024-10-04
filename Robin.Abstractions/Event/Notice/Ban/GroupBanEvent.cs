namespace Robin.Abstractions.Event.Notice.Ban;

[EventDescription("群禁言")]
public abstract record GroupBanEvent(
    long Time,
    long GroupId,
    long OperatorId,
    long UserId,
    long Duration
) : NoticeEvent(Time);
