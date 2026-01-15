using System.Text.Json;
using System.Text.Json.Serialization;
using Robin.Abstractions.Message;
using Robin.Abstractions.Message.Entity;
using Robin.Implementations.OneBot.Converter;

namespace Robin.Implementations.OneBot.Entity.Message.Data;

[Serializable]
[OneBotSegmentData("json", typeof(JsonData))]
internal class OneBotJsonData : IOneBotSegmentData
{
    [JsonPropertyName("data")]
    public required string Data { get; set; }

    public SegmentData ToSegmentData(OneBotMessageConverter converter) => new JsonData(Data);

    public OneBotSegment FromSegmentData(SegmentData data, OneBotMessageConverter converter)
    {
        var d = data as JsonData;
        Data = d!.Content;
        return new OneBotSegment { Type = "json", Data = JsonSerializer.SerializeToNode(this) };
    }
}
