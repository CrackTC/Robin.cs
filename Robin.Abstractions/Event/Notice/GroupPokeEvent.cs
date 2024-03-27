namespace Robin.Abstractions.Event.Notice;

public record GroupPokeEvent(
    long Time,
    long GroupId,
    long SenderId,
    long TargetId
) : NoticeEvent(Time);