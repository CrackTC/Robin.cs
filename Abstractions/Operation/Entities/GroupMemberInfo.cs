namespace Robin.Abstractions.Operation.Entities;

public record GroupMemberInfo(
    long GroupId,
    long UserId,
    string Nickname,
    string Card,
    UserSex Sex,
    int Age,
    string Area,
    long JoinTime,
    long LastSentTime,
    string Level,
    GroupMemberRole Role,
    bool Unfriendly,
    string Title,
    long TitleExpireTime,
    bool CardChangable);