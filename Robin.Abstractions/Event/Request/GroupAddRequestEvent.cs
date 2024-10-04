namespace Robin.Abstractions.Event.Request;

[EventDescription("加群请求")]
public record GroupAddRequestEvent(
    long Time,
    long UserId,
    long GroupId,
    string Comment,
    string Flag
) : GroupRequestEvent(Time, UserId, GroupId, Comment, Flag);
