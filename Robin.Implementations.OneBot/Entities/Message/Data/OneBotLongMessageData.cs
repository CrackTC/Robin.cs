using System.Text.Json;
using System.Text.Json.Serialization;
using Robin.Abstractions.Message;
using Robin.Abstractions.Message.Entities;
using Robin.Implementations.OneBot.Converters;

namespace Robin.Implementations.OneBot.Entities.Message.Data;

[Serializable]
[OneBotSegmentData("longmsg", typeof(LongMessageData))]
internal class OneBotLongMessageData : IOneBotSegmentData
{
    [JsonPropertyName("id")] public required string Id { get; set; }

    public SegmentData ToSegmentData(OneBotMessageConverter converter) => new LongMessageData(Id);

    public OneBotSegment FromSegmentData(SegmentData data, OneBotMessageConverter converter)
    {
        var d = data as LongMessageData;
        Id = d!.Id;
        return new OneBotSegment { Type = "longmsg", Data = JsonSerializer.SerializeToNode(this) };
    }
}