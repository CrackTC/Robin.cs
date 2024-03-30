using System.Text.Json;
using Robin.Abstractions.Message;
using Robin.Abstractions.Message.Entities;
using Robin.Implementations.OneBot.Converters;

namespace Robin.Implementations.OneBot.Entities.Message.Data;

[Serializable]
[OneBotSegmentData("shake", typeof(ShakeData))]
internal class OneBotShakeData : IOneBotSegmentData
{
    public SegmentData ToSegmentData(OneBotMessageConverter _) => new ShakeData();
    public OneBotSegment FromSegmentData(SegmentData data, OneBotMessageConverter converter)
        => new() { Type = "shake", Data = JsonSerializer.SerializeToNode(this) };
}