using System.Text.Json;
using System.Text.Json.Serialization;
using Robin.Abstractions.Message;
using Robin.Abstractions.Message.Entity;
using Robin.Implementations.OneBot.Converter;

namespace Robin.Implementations.OneBot.Entity.Message.Data;

[Serializable]
[OneBotSegmentData("text", typeof(TextData))]
internal class OneBotTextData : IOneBotSegmentData
{
    [JsonPropertyName("text")] public required string Text { get; set; }

    public SegmentData ToSegmentData(OneBotMessageConverter _) => new TextData(Text);
    public OneBotSegment FromSegmentData(SegmentData data, OneBotMessageConverter converter)
    {
        var d = data as TextData;
        Text = d!.Text;
        return new OneBotSegment { Type = "text", Data = JsonSerializer.SerializeToNode(this) };
    }
}
