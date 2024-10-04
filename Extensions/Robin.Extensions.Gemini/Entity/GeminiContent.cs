using System.Text.Json.Serialization;

namespace Robin.Extensions.Gemini.Entity;

[Serializable]
internal class GeminiContent
{
    [JsonPropertyName("role")] public GeminiRole? Role { get; set; }
    [JsonPropertyName("parts")] public required List<GeminiPart> Parts { get; set; }
}
