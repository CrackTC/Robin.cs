namespace Robin.Abstractions.Event.Notice.Member.Decrease;

public record GroupKickedEvent(
    long Time,
    long GroupId,
    long OperatorId,
    long UserId
) : GroupDecreaseEvent(Time, GroupId, OperatorId, UserId);