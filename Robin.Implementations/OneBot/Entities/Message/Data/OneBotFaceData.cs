using System.Text.Json;
using System.Text.Json.Serialization;
using Robin.Abstractions.Message;
using Robin.Abstractions.Message.Entities;
using Robin.Implementations.OneBot.Converters;

namespace Robin.Implementations.OneBot.Entities.Message.Data;

[Serializable]
[OneBotSegmentData("face", typeof(FaceData))]
internal class OneBotFaceData : IOneBotSegmentData
{
    [JsonPropertyName("id")] public required string Id { get; set; }

    public SegmentData ToSegmentData(OneBotMessageConverter _) => new FaceData(Id);
    public OneBotSegment FromSegmentData(SegmentData data, OneBotMessageConverter converter)
    {
        var d = data as FaceData;
        Id = d!.Id;
        return new OneBotSegment { Type = "face", Data = JsonSerializer.SerializeToNode(this) };
    }
}