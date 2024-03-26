namespace Robin.Abstractions.Event.Notice.Member;

public abstract record GroupMemberEvent(
    long Time,
    long GroupId,
    long OperatorId,
    long UserId
) : BotEvent(Time);