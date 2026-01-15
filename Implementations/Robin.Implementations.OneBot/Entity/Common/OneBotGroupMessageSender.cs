using System.Text.Json.Serialization;
using Robin.Abstractions.Entity;

namespace Robin.Implementations.OneBot.Entity.Common;

internal class OneBotGroupMessageSender : OneBotMessageSender
{
    [JsonPropertyName("card")] public string? Card { get; set; }
    [JsonPropertyName("area")] public string? Area { get; set; }
    [JsonPropertyName("level")] public string? Level { get; set; }
    [JsonPropertyName("role")] public OneBotGroupRole Role { get; set; }
    [JsonPropertyName("title")] public string? Title { get; set; }

    public GroupMessageSender ToGroupMessageSender() => new(
            UserId,
            Nickname,
            Card,
            Sex.ToUserSex(),
            Age,
            Area,
            Level is not null ? Convert.ToInt32(Level) : null,
            Role.ToGroupMemberRole(),
            Title
        );
}
