namespace Robin.Abstractions.Event.Notice.Ban;

[EventDescription("群禁言设置")]
public record GroupSetBanEvent(
    long Time,
    long GroupId,
    long OperatorId,
    long UserId,
    long Duration
) : GroupBanEvent(Time, GroupId, OperatorId, UserId, Duration);