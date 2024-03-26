namespace Robin.Abstractions.Event.Notice.Admin;

public record GroupAdminUnsetEvent(
    long Time,
    long GroupId,
    long UserId
) : GroupAdminEvent(Time, GroupId, UserId);