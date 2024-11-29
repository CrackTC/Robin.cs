namespace Robin.Abstractions.Event.Notice;

[EventDescription("好友已添加")]
public record FriendAddEvent(
    long Time,
    long UserId
) : NoticeEvent(Time), IPrivateEvent;
