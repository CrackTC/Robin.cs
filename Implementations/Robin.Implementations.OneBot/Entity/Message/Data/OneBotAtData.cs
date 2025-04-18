using System.Text.Json;
using System.Text.Json.Serialization;
using Robin.Abstractions.Message;
using Robin.Abstractions.Message.Entity;
using Robin.Implementations.OneBot.Converter;

namespace Robin.Implementations.OneBot.Entity.Message.Data;

[Serializable]
[OneBotSegmentData("at", typeof(AtData))]
internal class OneBotAtData : IOneBotSegmentData
{
    [JsonPropertyName("qq")] public required string Uin { get; set; }

    public SegmentData ToSegmentData(OneBotMessageConverter _) =>
        new AtData(Uin is "all" ? 0 : Convert.ToInt64(Uin));

    public OneBotSegment FromSegmentData(SegmentData data, OneBotMessageConverter converter)
    {
        var d = data as AtData;
        Uin = d!.Uin.ToString();
        return new OneBotSegment { Type = "at", Data = JsonSerializer.SerializeToNode(this) };
    }
}
