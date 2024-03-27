namespace Robin.Abstractions.Event.Notice;

public record FriendAddEvent(
    long Time,
    long UserId
) : NoticeEvent(Time);