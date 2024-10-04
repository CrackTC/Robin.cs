namespace Robin.Abstractions.Event.Notice.Member.Decrease;

[EventDescription("自身被踢")]
public record GroupKickedEvent(
    long Time,
    long GroupId,
    long OperatorId,
    long UserId
) : GroupDecreaseEvent(Time, GroupId, OperatorId, UserId);
