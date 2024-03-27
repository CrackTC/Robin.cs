namespace Robin.Abstractions.Event.Notice.Honor;

public record GroupEmotionEvent(
    long Time,
    long GroupId,
    long UserId
) : GroupHonorEvent(Time, GroupId, UserId);