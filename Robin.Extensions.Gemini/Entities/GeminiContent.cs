using System.Text.Json.Serialization;

namespace Robin.Extensions.Gemini.Entities;

[Serializable]
internal class GeminiContent
{
    [JsonPropertyName("role")] public required GeminiRole Role { get; set; }
    [JsonPropertyName("parts")] public required List<GeminiTextPart> Parts { get; set; }
}