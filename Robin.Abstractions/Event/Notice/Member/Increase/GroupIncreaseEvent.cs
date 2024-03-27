namespace Robin.Abstractions.Event.Notice.Member.Increase;

public abstract record GroupIncreaseEvent(
    long Time,
    long GroupId,
    long OperatorId,
    long UserId
) : GroupMemberEvent(Time, GroupId, OperatorId, UserId);