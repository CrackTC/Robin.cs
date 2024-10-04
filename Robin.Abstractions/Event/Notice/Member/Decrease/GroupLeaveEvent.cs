namespace Robin.Abstractions.Event.Notice.Member.Decrease;

[EventDescription("群成员退群")]
public record GroupLeaveEvent(
    long Time,
    long GroupId,
    long OperatorId,
    long UserId
) : GroupMemberEvent(Time, GroupId, OperatorId, UserId);
