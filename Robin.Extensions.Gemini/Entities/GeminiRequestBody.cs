using System.Text.Json.Serialization;

namespace Robin.Extensions.Gemini.Entities;

[Serializable]
internal class GeminiRequestBody
{
    [JsonPropertyName("contents")] public required List<GeminiContent> Contents { get; set; }
}