using System.Text.Json;
using System.Text.Json.Serialization;
using Robin.Abstractions.Message;
using Robin.Abstractions.Message.Entity;
using Robin.Implementations.OneBot.Converter;

namespace Robin.Implementations.OneBot.Entity.Message.Data;

[OneBotSegmentData("anonymous", typeof(AnonymousData))]
internal class OneBotAnonymousData : IOneBotSegmentData
{
    [JsonPropertyName("ignore")]
    public string? Ignore { get; set; }

    public SegmentData ToSegmentData(OneBotMessageConverter _) => new AnonymousData(Ignore is "1");

    public OneBotSegment FromSegmentData(SegmentData data, OneBotMessageConverter converter)
    {
        var d = data as AnonymousData;
        Ignore = d!.Ignore is true ? "1" : "0";
        return new OneBotSegment
        {
            Type = "anonymous",
            Data = JsonSerializer.SerializeToNode(this),
        };
    }
}
