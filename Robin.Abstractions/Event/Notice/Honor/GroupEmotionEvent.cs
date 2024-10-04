namespace Robin.Abstractions.Event.Notice.Honor;

[EventDescription("群荣誉：快乐源泉")]
public record GroupEmotionEvent(
    long Time,
    long GroupId,
    long UserId
) : GroupHonorEvent(Time, GroupId, UserId);
