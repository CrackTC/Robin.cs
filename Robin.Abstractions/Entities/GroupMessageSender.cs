namespace Robin.Abstractions.Entities;

public record GroupMessageSender(
    long UserId,
    string Nickname,
    string Card,
    UserSex Sex,
    int Age,
    string Area,
    string Level,
    GroupMemberRole Role,
    string Title
) : MessageSender(UserId, Nickname, Sex, Age);