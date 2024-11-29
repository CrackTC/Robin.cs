namespace Robin.Abstractions.Event.Notice.Honor;

[EventDescription("群荣誉事件")]
public abstract record GroupHonorEvent(
    long Time,
    long GroupId,
    long UserId
) : NoticeEvent(Time), IGroupEvent;
