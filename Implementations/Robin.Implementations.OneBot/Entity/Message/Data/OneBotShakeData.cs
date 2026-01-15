using System.Text.Json;
using Robin.Abstractions.Message;
using Robin.Abstractions.Message.Entity;
using Robin.Implementations.OneBot.Converter;

namespace Robin.Implementations.OneBot.Entity.Message.Data;

[OneBotSegmentData("shake", typeof(ShakeData))]
internal class OneBotShakeData : IOneBotSegmentData
{
    public SegmentData ToSegmentData(OneBotMessageConverter _) => new ShakeData();

    public OneBotSegment FromSegmentData(SegmentData data, OneBotMessageConverter converter) =>
        new() { Type = "shake", Data = JsonSerializer.SerializeToNode(this) };
}
