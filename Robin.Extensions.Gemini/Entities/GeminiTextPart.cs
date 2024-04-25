using System.Text.Json.Serialization;

namespace Robin.Extensions.Gemini.Entities;

[Serializable]
internal class GeminiTextPart : GeminiPart
{
    [JsonPropertyName("text")] public required string Text { get; set; }
}