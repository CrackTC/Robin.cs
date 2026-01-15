using System.Text.Json.Serialization;

namespace Robin.Extensions.Gemini.Entity.Responses;

internal class GeminiGenerateDataResponse : GeminiResponse
{
    [JsonPropertyName("candidates")]
    public required List<GeminiCandidate> Candidates { get; set; }
}
