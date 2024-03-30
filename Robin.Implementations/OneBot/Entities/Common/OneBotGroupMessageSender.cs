using System.Text.Json.Serialization;

namespace Robin.Implementations.OneBot.Entities.Common;

[Serializable]
public class OneBotGroupMessageSender : OneBotMessageSender
{
    [JsonPropertyName("card")] public required string Card { get; set; }
    [JsonPropertyName("area")] public required string Area { get; set; }
    [JsonPropertyName("level")] public required string Level { get; set; }
    [JsonPropertyName("role")] public required string Role { get; set; }
    [JsonPropertyName("title")] public required string Title { get; set; }
}