namespace Robin.Abstractions.Event.Notice.Reaction;

[EventDescription("添加表情回应")]
public record ReactionAddEvent(
    long Time,
    string MessageId,
    long GroupId,
    long OperatorId,
    string Code,
    uint Count
) : ReactionEvent(Time, MessageId, GroupId, OperatorId, Code, Count);
