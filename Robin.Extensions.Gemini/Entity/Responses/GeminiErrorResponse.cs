using System.Text.Json.Serialization;

namespace Robin.Extensions.Gemini.Entity.Responses;

[Serializable]
internal class GeminiErrorResponse : GeminiResponse
{
    [JsonPropertyName("error")] public required GeminiError Error { get; set; }
}