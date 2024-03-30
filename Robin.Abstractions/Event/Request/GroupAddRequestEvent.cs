namespace Robin.Abstractions.Event.Request;

public record GroupAddRequestEvent(
    long Time,
    long UserId,
    long GroupId,
    string Comment,
    string Flag
) : GroupRequestEvent(Time, UserId, GroupId, Comment, Flag);