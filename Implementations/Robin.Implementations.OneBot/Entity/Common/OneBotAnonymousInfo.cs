using System.Text.Json.Serialization;

namespace Robin.Implementations.OneBot.Entity.Common;

[Serializable]
internal class OneBotAnonymousInfo
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("flag")] public string Flag { get; set; } = string.Empty;
}
