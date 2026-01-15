namespace Robin.Abstractions.Event.Notice.Member.Increase;

[EventDescription("群成员邀请进群")]
public record GroupInviteEvent(long Time, long GroupId, long OperatorId, long UserId)
    : GroupIncreaseEvent(Time, GroupId, OperatorId, UserId);
