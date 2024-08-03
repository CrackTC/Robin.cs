namespace Robin.Abstractions.Event.Notice;

[EventDescription("群红包运气王")]
public record LuckyKingEvent(
    long Time,
    long GroupId,
    long SenderId,
    long TargetId
) : NoticeEvent(Time);