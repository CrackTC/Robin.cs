namespace Robin.Abstractions.Event.Notice.Admin;

[EventDescription("群管理员变更")]
public record GroupAdminEvent(long Time, long GroupId, long UserId)
    : NoticeEvent(Time),
        IGroupEvent;
