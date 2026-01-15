namespace Robin.Abstractions.Event.Notice;

[EventDescription("私聊戳一戳")]
public record FriendPokeEvent(long Time, long UserId, long SenderId, long TargetId)
    : NoticeEvent(Time),
        IPrivateEvent;
