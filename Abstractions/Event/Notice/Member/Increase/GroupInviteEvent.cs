namespace Robin.Abstractions.Event.Notice.Member.Increase;

public record GroupInviteEvent(
    long Time,
    long GroupId,
    long OperatorId,
    long UserId
) : GroupIncreaseEvent(Time, GroupId, OperatorId, UserId);