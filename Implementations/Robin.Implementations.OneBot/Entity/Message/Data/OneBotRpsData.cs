using System.Text.Json;
using Robin.Abstractions.Message;
using Robin.Abstractions.Message.Entity;
using Robin.Implementations.OneBot.Converter;

namespace Robin.Implementations.OneBot.Entity.Message.Data;

[Serializable]
[OneBotSegmentData("rps", typeof(RpsData))]
internal class OneBotRpsData : IOneBotSegmentData
{
    public SegmentData ToSegmentData(OneBotMessageConverter _) => new RpsData();

    public OneBotSegment FromSegmentData(SegmentData data, OneBotMessageConverter converter) =>
        new() { Type = "rps", Data = JsonSerializer.SerializeToNode(this) };
}