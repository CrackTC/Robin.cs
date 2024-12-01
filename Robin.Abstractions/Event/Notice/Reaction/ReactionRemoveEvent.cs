namespace Robin.Abstractions.Event.Notice.Reaction;

[EventDescription("删除表情回应")]
public record ReactionRemoveEvent(
    long Time,
    string MessageId,
    long GroupId,
    long OperatorId,
    string Code,
    uint Count
) : ReactionEvent(Time, MessageId, GroupId, OperatorId, Code, Count);
