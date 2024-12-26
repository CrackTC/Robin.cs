namespace Robin.Abstractions.Event.Notice.Recall;

[EventDescription("群消息撤回")]
public record GroupRecallEvent(
    long Time,
    long UserId,
    string MessageId,
    long GroupId,
    long OperatorId
) : RecallEvent(Time, UserId, MessageId), IGroupEvent;
