namespace Robin.Abstractions.Event.Notice.Honor;

public record GroupPerformerEvent(
    long Time,
    long GroupId,
    long UserId
) : GroupHonorEvent(Time, GroupId, UserId);