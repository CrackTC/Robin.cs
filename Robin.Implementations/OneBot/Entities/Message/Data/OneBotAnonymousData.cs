using System.Text.Json;
using System.Text.Json.Serialization;
using Robin.Abstractions.Message;
using Robin.Abstractions.Message.Entities;
using Robin.Implementations.OneBot.Converters;

namespace Robin.Implementations.OneBot.Entities.Message.Data;

[Serializable]
[OneBotSegmentData("anonymous", typeof(AnonymousData))]
internal class OneBotAnonymousData : IOneBotSegmentData
{
    [JsonPropertyName("ignore")] public string? Ignore { get; set; }

    public SegmentData ToSegmentData(OneBotMessageConverter _) => new AnonymousData(Ignore is "1");

    public OneBotSegment FromSegmentData(SegmentData data, OneBotMessageConverter converter)
    {
        var d = data as AnonymousData;
        Ignore = d!.Ignore is true ? "1" : "0";
        return new OneBotSegment { Type = "ignore", Data = JsonSerializer.SerializeToNode(this) };
    }
}