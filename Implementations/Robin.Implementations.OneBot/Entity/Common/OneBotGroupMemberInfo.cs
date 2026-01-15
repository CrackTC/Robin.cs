using System.Text.Json.Serialization;
using Robin.Abstractions.Entity;

namespace Robin.Implementations.OneBot.Entity.Common;

internal class OneBotGroupMemberInfo
{
    [JsonPropertyName("group_id")] public long GroupId { get; set; }
    [JsonPropertyName("user_id")] public long UserId { get; set; }
    [JsonPropertyName("nickname")] public required string Nickname { get; set; }
    [JsonPropertyName("card")] public required string Card { get; set; }
    [JsonPropertyName("sex")] public required OneBotSex Sex { get; set; }
    [JsonPropertyName("age")] public int Age { get; set; }
    [JsonPropertyName("area")] public required string Area { get; set; }
    [JsonPropertyName("join_time")] public int JoinTime { get; set; }
    [JsonPropertyName("last_sent_time")] public int LastSentTime { get; set; }
    [JsonPropertyName("level")] public required string Level { get; set; }
    [JsonPropertyName("role")] public required OneBotGroupRole Role { get; set; }
    [JsonPropertyName("unfriendly")] public bool Unfriendly { get; set; }
    [JsonPropertyName("title")] public required string Title { get; set; }
    [JsonPropertyName("title_expire_time")] public int TitleExpireTime { get; set; }
    [JsonPropertyName("card_changeable")] public bool CardChangeable { get; set; }

    public GroupMemberInfo ToGroupMemberInfo() => new(
        GroupId,
        UserId,
        Nickname,
        Card,
        Sex.ToUserSex(),
        Age,
        Area,
        JoinTime,
        LastSentTime,
        string.IsNullOrEmpty(Level) ? null : Convert.ToInt32(Level),
        Role.ToGroupMemberRole(),
        Unfriendly,
        Title,
        TitleExpireTime,
        CardChangeable
    );
}