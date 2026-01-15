using System.Text.Json;
using System.Text.Json.Serialization;
using Robin.Abstractions.Message;
using Robin.Abstractions.Message.Entity;
using Robin.Implementations.OneBot.Converter;

namespace Robin.Implementations.OneBot.Entity.Message.Data;

[OneBotSegmentData("xml", typeof(XmlData))]
internal class OneBotXmlData : IOneBotSegmentData
{
    [JsonPropertyName("data")]
    public required string Data { get; set; }

    public SegmentData ToSegmentData(OneBotMessageConverter converter) => new XmlData(Data);

    public OneBotSegment FromSegmentData(SegmentData data, OneBotMessageConverter converter)
    {
        var d = data as XmlData;
        Data = d!.Content;
        return new OneBotSegment { Type = "xml", Data = JsonSerializer.SerializeToNode(this) };
    }
}
