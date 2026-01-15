using System.Text.Json.Serialization;
using Robin.Abstractions.Entity;

namespace Robin.Implementations.OneBot.Entity.Common;

internal class OneBotUploadFile
{
    [JsonPropertyName("id")] public required string Id { get; set; }
    [JsonPropertyName("name")] public required string Name { get; set; }
    [JsonPropertyName("size")] public long Size { get; set; }
    [JsonPropertyName("busid")] public long BusId { get; set; }

    public UploadFile ToUploadFile() => new(Id, Name, Size, BusId);
}
