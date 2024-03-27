namespace Robin.Abstractions.Event.Notice.Honor;

public abstract record GroupHonorEvent(
    long Time,
    long GroupId,
    long UserId
) : NoticeEvent(Time);