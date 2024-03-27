namespace Robin.Abstractions.Event.Notice.Member.Decrease;

public abstract record GroupDecreaseEvent(
    long Time,
    long GroupId,
    long OperatorId,
    long UserId
) : GroupMemberEvent(Time, GroupId, OperatorId, UserId);