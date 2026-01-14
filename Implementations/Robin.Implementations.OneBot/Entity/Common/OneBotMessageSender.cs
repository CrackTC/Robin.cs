using System.Text.Json.Serialization;

namespace Robin.Implementations.OneBot.Entity.Common;

[Serializable]
public class OneBotMessageSender
{
    [JsonPropertyName("user_id")] public long UserId { get; set; }
    [JsonPropertyName("nickname")] public required string Nickname { get; set; }
    [JsonPropertyName("sex")] public string? Sex { get; set; }
    [JsonPropertyName("age")] public int Age { get; set; }
}
