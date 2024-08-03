namespace Robin.Abstractions.Event.Request;

[EventDescription("好友请求")]
public record FriendRequestEvent(
    long Time,
    long UserId,
    string Comment,
    string Flag
) : RequestEvent(Time, UserId, Comment, Flag);