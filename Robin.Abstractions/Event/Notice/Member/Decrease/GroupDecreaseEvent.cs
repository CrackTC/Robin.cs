namespace Robin.Abstractions.Event.Notice.Member.Decrease;

[EventDescription("群成员减少")]
public abstract record GroupDecreaseEvent(long Time, long GroupId, long OperatorId, long UserId)
    : GroupMemberEvent(Time, GroupId, OperatorId, UserId);
