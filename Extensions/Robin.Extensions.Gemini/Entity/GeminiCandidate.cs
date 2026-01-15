using System.Text.Json.Serialization;

namespace Robin.Extensions.Gemini.Entity;

internal class GeminiCandidate
{
    [JsonPropertyName("content")]
    public required GeminiContent Content { get; set; }
}
