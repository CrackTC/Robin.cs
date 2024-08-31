using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Robin.Implementations.OneBot.Network.WebSocket;

[Serializable]
internal class OneBotWebSocketRequest
{
    [JsonPropertyName("action")] public required string Action { get; set; }
    [JsonPropertyName("params")] public required JsonNode Params { get; set; }
    [JsonPropertyName("echo")] public required string Echo { get; set; }
}