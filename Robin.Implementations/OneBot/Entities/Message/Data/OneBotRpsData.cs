using System.Text.Json;
using Robin.Abstractions.Message;
using Robin.Abstractions.Message.Entities;
using Robin.Implementations.OneBot.Converters;

namespace Robin.Implementations.OneBot.Entities.Message.Data;

[Serializable]
[OneBotSegmentData("rps", typeof(RpsData))]
internal class OneBotRpsData : IOneBotSegmentData
{
    public SegmentData ToSegmentData(OneBotMessageConverter _) => new RpsData();

    public OneBotSegment FromSegmentData(SegmentData data, OneBotMessageConverter converter) =>
        new() { Type = "rps", Data = JsonSerializer.SerializeToNode(this) };
}