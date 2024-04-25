using System.Text.Json.Serialization;

namespace Robin.Extensions.Gemini.Entities;

[Serializable]
internal class GeminiCandidate
{
    [JsonPropertyName("content")] public required GeminiContent Content { get; set; }
}