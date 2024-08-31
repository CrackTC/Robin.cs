using System.Text.Json.Serialization;

namespace Robin.Extensions.Gemini.Entity;

[Serializable]
internal class GeminiRequestBody
{
    [JsonPropertyName("contents")] public required List<GeminiContent> Contents { get; set; }
    [JsonPropertyName("systemInstruction")] public GeminiContent? SystemInstruction { get; set; }
}