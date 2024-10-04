namespace Robin.Abstractions.Event.Notice.Member.Increase;

[EventDescription("群成员增加")]
public abstract record GroupIncreaseEvent(
    long Time,
    long GroupId,
    long OperatorId,
    long UserId
) : GroupMemberEvent(Time, GroupId, OperatorId, UserId);
