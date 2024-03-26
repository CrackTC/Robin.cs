namespace Robin.Abstractions.Event.Request;

public record FriendRequestEvent(
    long Time,
    long UserId,
    string Comment,
    string Flag
) : RequestEvent(Time, UserId, Comment, Flag);