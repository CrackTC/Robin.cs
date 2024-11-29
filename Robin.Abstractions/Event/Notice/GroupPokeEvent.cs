namespace Robin.Abstractions.Event.Notice;

[EventDescription("群聊戳一戳")]
public record GroupPokeEvent(
    long Time,
    long GroupId,
    long SenderId,
    long TargetId
) : NoticeEvent(Time), IGroupEvent;
