namespace Robin.Abstractions.Event.Notice.Admin;

[EventDescription("群管理员增加")]
public record GroupAdminSetEvent(
    long Time,
    long GroupId,
    long UserId
) : GroupAdminEvent(Time, GroupId, UserId);