namespace Robin.Abstractions.Entity;

public record GroupInfo(
    long GroupId,
    string GroupName,
    int MemberCount,
    int MaxMemberCount
);
