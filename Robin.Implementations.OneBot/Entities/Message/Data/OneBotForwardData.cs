using System.Text.Json;
using System.Text.Json.Serialization;
using Robin.Abstractions.Message;
using Robin.Abstractions.Message.Entities;
using Robin.Implementations.OneBot.Converters;

namespace Robin.Implementations.OneBot.Entities.Message.Data;

[Serializable]
[OneBotSegmentData("forward", typeof(ForwardData))]
internal class OneBotForwardData : IOneBotSegmentData
{
    [JsonPropertyName("id")] public required string Id { get; set; }

    public SegmentData ToSegmentData(OneBotMessageConverter _) => new ForwardData(Id);
    public OneBotSegment FromSegmentData(SegmentData data, OneBotMessageConverter converter)
    {
        var d = data as ForwardData;
        Id = d!.Id;
        return new OneBotSegment { Type = "forward", Data = JsonSerializer.SerializeToNode(this) };
    }
}