namespace Robin.Abstractions.Event.Notice.Member;

[EventDescription("群成员事件")]
public abstract record GroupMemberEvent(long Time, long GroupId, long OperatorId, long UserId)
    : NoticeEvent(Time),
        IGroupEvent;
