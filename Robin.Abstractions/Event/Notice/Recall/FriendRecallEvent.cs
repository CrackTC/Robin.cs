namespace Robin.Abstractions.Event.Notice.Recall;

[EventDescription("好友消息撤回")]
public record FriendRecallEvent(
    long Time,
    long UserId,
    long MessageId
) : RecallEvent(Time, UserId, MessageId);
