namespace Robin.Abstractions.Entity;

public record GroupInfo(
    long GroupId,
    string GroupName,
    int memberCount,
    int maxMemberCount
);
