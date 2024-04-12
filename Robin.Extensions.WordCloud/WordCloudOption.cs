using System.Text.Json.Serialization;

namespace Robin.Extensions.WordCloud;

[Serializable]
public record CloudOption
{
    [JsonPropertyName("width")] public int Width { get; set; }
    [JsonPropertyName("height")] public int Height { get; set; }
    [JsonPropertyName("colors")] public required List<string> Colors { get; set; }
    [JsonPropertyName("font_url")] public required string FontUrl { get; set; }
    [JsonPropertyName("padding")] public required int Padding { get; set; }
    [JsonPropertyName("background_image_blur")] public int BackgroundImageBlur { get; set; }
    [JsonPropertyName("background_image_url")] public required string BackgroundImageUrl { get; set; }

    // Not used for configuration
    [JsonPropertyName("text")] public required string Text { get; set; }
}

[Serializable]
public class WordCloudOption
{
    public required string ApiAddress { get; set; }
    public required CloudOption CloudOption { get; set; }
}