using System.Text.Json.Serialization;

namespace Robin.Extensions.WordCloud;

[Serializable]
public record CloudOption
{
    [JsonPropertyName("width")] public int Width { get; set; }
    [JsonPropertyName("height")] public int Height { get; set; }
    [JsonPropertyName("colors")] public required List<string> Colors { get; set; }
    [JsonPropertyName("font_path")] public required string FontPath { get; set; }
    [JsonPropertyName("padding")] public required int Padding { get; set; }
    [JsonPropertyName("background_image_blur")] public int BackgroundImageBlur { get; set; }

    // Not used for configuration
    [JsonPropertyName("text")] public required string Text { get; set; }
    [JsonPropertyName("background_image")] public required string BackgroundImage { get; set; }
}

[Serializable]
public class WordCloudOption
{
    public string Cron { get; set; } = "1 0 0 * * *";
    public required string ApiAddress { get; set; }
    public required CloudOption CloudOption { get; set; }
    public required string BackgroundImagePath { get; set; }
}