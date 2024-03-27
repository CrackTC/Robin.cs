namespace Robin.Abstractions.Event.Notice;

public record LuckyKingEvent(
    long Time,
    long GroupId,
    long SenderId,
    long TargetId
) : NoticeEvent(Time);