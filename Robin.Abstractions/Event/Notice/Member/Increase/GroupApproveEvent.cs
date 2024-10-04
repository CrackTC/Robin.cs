namespace Robin.Abstractions.Event.Notice.Member.Increase;

[EventDescription("管理员同意加群")]
public record GroupApproveEvent(
    long Time,
    long GroupId,
    long OperatorId,
    long UserId
) : GroupIncreaseEvent(Time, GroupId, OperatorId, UserId);
