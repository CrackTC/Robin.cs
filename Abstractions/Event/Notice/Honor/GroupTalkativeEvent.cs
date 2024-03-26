namespace Robin.Abstractions.Event.Notice.Honor;

public record GroupTalkativeEvent(
    long Time,
    long GroupId,
    long UserId
) : GroupHonorEvent(Time, GroupId, UserId);