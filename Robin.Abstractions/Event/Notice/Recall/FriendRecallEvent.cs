namespace Robin.Abstractions.Event.Notice.Recall;

public record FriendRecallEvent(
    long Time,
    long UserId,
    long MessageId
) : RecallEvent(Time, UserId, MessageId);