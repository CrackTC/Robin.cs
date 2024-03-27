namespace Robin.Abstractions.Event.Notice.Admin;

public record GroupAdminEvent(
    long Time,
    long GroupId,
    long UserId
) : NoticeEvent(Time);