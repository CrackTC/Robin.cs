namespace Robin.Abstractions.Event.Request;

public abstract record GroupRequestEvent(
    long Time,
    long UserId,
    long GroupId,
    string Comment,
    string Flag
) : RequestEvent(Time, UserId, Comment, Flag);