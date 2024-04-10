using System.Text.Json;
using Robin.Abstractions.Message;
using Robin.Abstractions.Message.Entities;
using Robin.Implementations.OneBot.Converters;

namespace Robin.Implementations.OneBot.Entities.Message.Data;

[Serializable]
[OneBotSegmentData("dice", typeof(DiceData))]
internal class OneBotDiceData : IOneBotSegmentData
{
    public SegmentData ToSegmentData(OneBotMessageConverter _) => new DiceData();

    public OneBotSegment FromSegmentData(SegmentData data, OneBotMessageConverter converter) =>
        new() { Type = "dice", Data = JsonSerializer.SerializeToNode(this) };
}