namespace Robin.Abstractions.Event.Notice.Admin;

[EventDescription("群管理员减少")]
public record GroupAdminUnsetEvent(long Time, long GroupId, long UserId)
    : GroupAdminEvent(Time, GroupId, UserId);
