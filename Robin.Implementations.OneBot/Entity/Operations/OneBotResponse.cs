using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Robin.Implementations.OneBot.Entity.Operations;

[Serializable]
internal class OneBotResponse
{
    [JsonPropertyName("status")] public string Status { get; set; } = string.Empty;
    [JsonPropertyName("retcode")] public int ReturnCode { get; set; }
    [JsonPropertyName("data")] public JsonNode? Data { get; set; }
    [JsonPropertyName("echo")] public string? Echo { get; set; }
}