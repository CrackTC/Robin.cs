using System.Text.Json;
using System.Text.Json.Serialization;
using Robin.Abstractions.Message;
using Robin.Abstractions.Message.Entity;
using Robin.Implementations.OneBot.Converter;

namespace Robin.Implementations.OneBot.Entity.Message.Data;

[Serializable]
[OneBotSegmentData("share", typeof(ShareData))]
internal class OneBotShareData : IOneBotSegmentData
{
    [JsonPropertyName("url")]
    public required string Url { get; set; }

    [JsonPropertyName("title")]
    public required string Title { get; set; }

    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("image")]
    public string? Image { get; set; }

    public SegmentData ToSegmentData(OneBotMessageConverter _) =>
        new ShareData(Url, Title, Content, Image);

    public OneBotSegment FromSegmentData(SegmentData data, OneBotMessageConverter converter)
    {
        var d = data as ShareData;
        Url = d!.Url;
        Title = d.Title;
        Content = d.Description;
        Image = d.ImageUrl;
        return new OneBotSegment { Type = "share", Data = JsonSerializer.SerializeToNode(this) };
    }
}
