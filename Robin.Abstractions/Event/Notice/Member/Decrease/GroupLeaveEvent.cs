namespace Robin.Abstractions.Event.Notice.Member.Decrease;

public record GroupLeaveEvent(
    long Time,
    long GroupId,
    long OperatorId,
    long UserId
) : GroupMemberEvent(Time, GroupId, OperatorId, UserId);