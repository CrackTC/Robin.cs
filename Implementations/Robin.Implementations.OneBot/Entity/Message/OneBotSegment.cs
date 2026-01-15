using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Robin.Implementations.OneBot.Entity.Message;

internal class OneBotSegment
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public JsonNode? Data { get; set; }
}
