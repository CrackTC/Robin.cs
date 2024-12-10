using System.Text.Json;
using System.Text.Json.Serialization;
using Robin.Abstractions.Message;
using Robin.Abstractions.Message.Entity;
using Robin.Implementations.OneBot.Converter;

namespace Robin.Implementations.OneBot.Entity.Message.Data;

[Serializable]
[OneBotSegmentData("record", typeof(RecordData))]
internal class OneBotRecordData : IOneBotSegmentData
{
    [JsonPropertyName("file")] public required string File { get; set; }
    [JsonPropertyName("magic")] public string? Magic { get; set; }
    [JsonPropertyName("url")] public string? Url { get; set; }
    [JsonPropertyName("cache")] public string? Cache { get; set; }
    [JsonPropertyName("proxy")] public string? Proxy { get; set; }
    [JsonPropertyName("timeout")] public string? Timeout { get; set; }

    public SegmentData ToSegmentData(OneBotMessageConverter _) =>
        new RecordData(File, Magic is "1", Url, Cache is not "0", Proxy is not "0",
            Timeout is not null ? Convert.ToDouble(Timeout) : null);

    public OneBotSegment FromSegmentData(SegmentData data, OneBotMessageConverter converter)
    {
        var d = data as RecordData;
        File = d!.File;
        Magic = d.IsMagic ? "1" : "0";
        Cache = d.UseCache is not false ? "1" : "0";
        Proxy = d.UseProxy is not false ? "1" : "0";
        Timeout = d.Timeout?.ToString();
        return new OneBotSegment { Type = "record", Data = JsonSerializer.SerializeToNode(this) };
    }
}
