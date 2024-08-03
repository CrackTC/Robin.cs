namespace Robin.Abstractions.Event.Request;

[EventDescription("群成员邀请加群请求")]
public record GroupInviteRequestEvent(
    long Time,
    long UserId,
    long GroupId,
    string Comment,
    string Flag
) : GroupRequestEvent(Time, UserId, GroupId, Comment, Flag);