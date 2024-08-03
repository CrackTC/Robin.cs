namespace Robin.Abstractions.Event.Notice.Honor;

[EventDescription("群荣誉：龙王")]
public record GroupTalkativeEvent(
    long Time,
    long GroupId,
    long UserId
) : GroupHonorEvent(Time, GroupId, UserId);