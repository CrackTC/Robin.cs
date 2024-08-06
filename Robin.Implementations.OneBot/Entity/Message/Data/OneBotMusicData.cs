using System.Text.Json;
using System.Text.Json.Serialization;
using Robin.Abstractions.Message;
using Robin.Abstractions.Message.Entity;
using Robin.Implementations.OneBot.Converter;

namespace Robin.Implementations.OneBot.Entity.Message.Data;

[Serializable]
[OneBotSegmentData("music", typeof(CustomMusicData), typeof(MusicData))]
internal class OneBotMusicData : IOneBotSegmentData
{
    [JsonPropertyName("type")] public required string Type { get; set; }
    [JsonPropertyName("id")] public required string Id { get; set; }
    [JsonPropertyName("url")] public required string Url { get; set; }
    [JsonPropertyName("audio")] public required string Audio { get; set; }
    [JsonPropertyName("title")] public required string Title { get; set; }
    [JsonPropertyName("content")] public string? Content { get; set; }
    [JsonPropertyName("image")] public string? Image { get; set; }

    public SegmentData ToSegmentData(OneBotMessageConverter _) =>
        Type switch
        {
            "custom" => new CustomMusicData(Url, Audio, Title, Content, Image),
            _ => new MusicData(Type, Id)
        };

    public OneBotSegment FromSegmentData(SegmentData data, OneBotMessageConverter converter)
    {
        switch (data)
        {
            case CustomMusicData d:
                Type = "custom";
                Url = d.Url;
                Audio = d.AudioUrl;
                Title = d.Title;
                Content = d.Description;
                Image = d.ImageUrl;
                return new OneBotSegment { Type = "music", Data = JsonSerializer.SerializeToNode(this) };
            case MusicData d:
                Type = d.Source;
                Id = d.Id;
                return new OneBotSegment { Type = "music", Data = JsonSerializer.SerializeToNode(this) };
            default:
                throw new Exception();
        }
    }
}