using System.Text.Json.Serialization;

namespace Robin.Extensions.Gemini.Entity;

public class GeminiPart
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("inlineData")]
    public GeminiBlob? InlineData { get; set; }
}
