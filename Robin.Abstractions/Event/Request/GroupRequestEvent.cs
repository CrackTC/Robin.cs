namespace Robin.Abstractions.Event.Request;

[EventDescription("加群请求")]
public abstract record GroupRequestEvent(
    long Time,
    long UserId,
    long GroupId,
    string Comment,
    string Flag
) : RequestEvent(Time, UserId, Comment, Flag);