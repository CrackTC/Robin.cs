namespace Robin.Abstractions.Event.Notice.Admin;

public record GroupAdminSetEvent(
    long Time,
    long GroupId,
    long UserId
) : GroupAdminEvent(Time, GroupId, UserId);