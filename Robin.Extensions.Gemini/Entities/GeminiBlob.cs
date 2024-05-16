using System.Text.Json.Serialization;

namespace Robin.Extensions.Gemini.Entities;

[Serializable]
public class GeminiBlob
{
    [JsonPropertyName("mimeType")] public required string MimeType { get; set; }
    [JsonPropertyName("data")] public required byte[] Data { get; set; }
}