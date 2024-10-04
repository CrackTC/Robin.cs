using System.Text.Json;
using System.Text.Json.Serialization;
using Robin.Abstractions.Message;
using Robin.Abstractions.Message.Entity;
using Robin.Implementations.OneBot.Converter;

namespace Robin.Implementations.OneBot.Entity.Message.Data;

[Serializable]
[OneBotSegmentData("reply", typeof(ReplyData))]
internal class OneBotReplyData : IOneBotSegmentData
{
    [JsonPropertyName("id")] public required string Id { get; set; }

    public SegmentData ToSegmentData(OneBotMessageConverter _) => new ReplyData(Id);
    public OneBotSegment FromSegmentData(SegmentData data, OneBotMessageConverter converter)
    {
        var d = data as ReplyData;
        Id = d!.Id;
        return new OneBotSegment { Type = "reply", Data = JsonSerializer.SerializeToNode(this) };
    }
}
