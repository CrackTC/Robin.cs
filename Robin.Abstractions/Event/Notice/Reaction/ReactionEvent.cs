namespace Robin.Abstractions.Event.Notice.Reaction;

[EventDescription("表情回应")]
public record ReactionEvent(
    long Time,
    string MessageId,
    long GroupId,
    long OperatorId,
    string Code,
    uint Count
) : NoticeEvent(Time), IGroupEvent;
