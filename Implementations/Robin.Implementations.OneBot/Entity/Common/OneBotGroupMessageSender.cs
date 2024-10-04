using System.Text.Json.Serialization;

namespace Robin.Implementations.OneBot.Entity.Common;

[Serializable]
public class OneBotGroupMessageSender : OneBotMessageSender
{
    [JsonPropertyName("card")] public string? Card { get; set; }
    [JsonPropertyName("area")] public string? Area { get; set; }
    [JsonPropertyName("level")] public string? Level { get; set; }
    [JsonPropertyName("role")] public string? Role { get; set; }
    [JsonPropertyName("title")] public string? Title { get; set; }
}
