using System.Text.Json.Serialization;

namespace Robin.Implementations.OneBot.Entity.Common;

[Serializable]
internal class OneBotUploadFile
{
    [JsonPropertyName("id")] public required string Id { get; set; }
    [JsonPropertyName("name")] public required string Name { get; set; }
    [JsonPropertyName("size")] public long Size { get; set; }
    [JsonPropertyName("busid")] public long BusId { get; set; }
}