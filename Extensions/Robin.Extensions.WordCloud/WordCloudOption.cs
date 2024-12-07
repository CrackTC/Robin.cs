using System.Text.Json.Serialization;

namespace Robin.Extensions.WordCloud;

[Serializable]
public record CloudOption
{
    // JsonPropertyName only used for serialization
    [JsonPropertyName("width")] public int Width { get; set; }
    [JsonPropertyName("height")] public int Height { get; set; }
    [JsonPropertyName("stroke_width")] public int StrokeWidth { get; set; }
    [JsonPropertyName("stroke_ratio")] public float StrokeRatio { get; set; }
    [JsonPropertyName("stroke_colors")] public List<string>? StrokeColors { get; set; }
    [JsonPropertyName("colors")] public List<string>? Colors { get; set; }
    [JsonPropertyName("padding")] public int Padding { get; set; }
    [JsonPropertyName("background_image_blur")] public int BackgroundImageBlur { get; set; }
    [JsonPropertyName("background_image_url")] public string? BackgroundImageUrl { get; set; }
    [JsonPropertyName("background_size_limit")] public int? BackgroundSizeLimit { get; set; }

    // Not used for configuration
    [JsonPropertyName("text")] public required string Text { get; set; }
}

[Serializable]
public class WordCloudOption
{
    public required string ApiAddress { get; set; }
    public required CloudOption CloudOption { get; set; }
}
