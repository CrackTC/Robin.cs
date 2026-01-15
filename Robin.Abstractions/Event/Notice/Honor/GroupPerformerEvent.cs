namespace Robin.Abstractions.Event.Notice.Honor;

[EventDescription("群荣誉：群聊之火")]
public record GroupPerformerEvent(long Time, long GroupId, long UserId)
    : GroupHonorEvent(Time, GroupId, UserId);
