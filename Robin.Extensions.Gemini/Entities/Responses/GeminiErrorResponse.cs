using System.Text.Json.Serialization;

namespace Robin.Extensions.Gemini.Entities.Responses;

[Serializable]
internal class GeminiErrorResponse : GeminiResponse
{
    [JsonPropertyName("error")] public required GeminiError Error { get; set; }
}