namespace Robin.Abstractions.Event.Notice.Recall;

public record GroupRecallEvent(
    long Time,
    long UserId,
    long MessageId,
    long GroupId,
    long OperatorId
) : RecallEvent(Time, UserId, MessageId);