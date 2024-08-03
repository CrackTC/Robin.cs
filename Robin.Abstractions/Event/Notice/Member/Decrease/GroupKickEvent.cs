namespace Robin.Abstractions.Event.Notice.Member.Decrease;

[EventDescription("群成员被踢")]
public record GroupKickEvent(
    long Time,
    long GroupId,
    long OperatorId,
    long UserId
) : GroupDecreaseEvent(Time, GroupId, OperatorId, UserId);