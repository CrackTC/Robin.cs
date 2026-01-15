using System.Text.Json.Serialization;

namespace Robin.Extensions.Gemini.Entity.Responses;

internal class GeminiErrorResponse : GeminiResponse
{
    [JsonPropertyName("error")]
    public required GeminiError Error { get; set; }
}
