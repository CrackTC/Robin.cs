using System.Text.Json;
using System.Text.Json.Serialization;
using Robin.Abstractions.Message;
using Robin.Abstractions.Message.Entity;
using Robin.Implementations.OneBot.Converter;

namespace Robin.Implementations.OneBot.Entity.Message.Data;

[Serializable]
[OneBotSegmentData("video", typeof(VideoData))]
internal class OneBotVideoData : IOneBotSegmentData
{
    [JsonPropertyName("file")]
    public required string File { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("cache")]
    public string? Cache { get; set; }

    [JsonPropertyName("proxy")]
    public string? Proxy { get; set; }

    [JsonPropertyName("timeout")]
    public string? Timeout { get; set; }

    public SegmentData ToSegmentData(OneBotMessageConverter _) =>
        new VideoData(
            File,
            Url,
            Cache is not "0",
            Proxy is not "0",
            Timeout is not null ? Convert.ToDouble(Timeout) : null
        );

    public OneBotSegment FromSegmentData(SegmentData data, OneBotMessageConverter converter)
    {
        var d = data as VideoData;
        File = d!.File;
        Url = d.Url;
        Cache = d.UseCache is not false ? "1" : "0";
        Proxy = d.UseProxy is not false ? "1" : "0";
        Timeout = d.Timeout?.ToString();
        return new OneBotSegment { Type = "video", Data = JsonSerializer.SerializeToNode(this) };
    }
}
