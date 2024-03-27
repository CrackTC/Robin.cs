namespace Robin.Abstractions.Event.Notice.Ban;

public record GroupSetBanEvent(
    long Time,
    long GroupId,
    long OperatorId,
    long UserId,
    long Duration
) : GroupBanEvent(Time, GroupId, OperatorId, UserId, Duration);