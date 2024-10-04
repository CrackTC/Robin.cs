namespace Robin.Abstractions.Event.Notice.Ban;

[EventDescription("群禁言解除")]
public record GroupUnsetBanEvent(
    long Time,
    long GroupId,
    long OperatorId,
    long UserId,
    long Duration
) : GroupBanEvent(Time, GroupId, OperatorId, UserId, Duration);
