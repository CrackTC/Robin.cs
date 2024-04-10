using System.Text.Json.Serialization;

namespace Robin.Implementations.OneBot.Entities.Common;

[Serializable]
public class OneBotStatus
{
    [JsonPropertyName("online")] public bool Online { get; set; }
    [JsonPropertyName("good")] public bool Good { get; set; }
}