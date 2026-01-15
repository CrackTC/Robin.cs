using System.Text.Json.Serialization;

namespace Robin.Extensions.Gemini.Entity;

internal class GeminiError
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public required string Message { get; set; }

    [JsonPropertyName("status")]
    public required string Status { get; set; }
}
