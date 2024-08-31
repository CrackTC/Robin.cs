using System.Text.Json;
using System.Text.Json.Serialization;
using Robin.Abstractions.Message;
using Robin.Abstractions.Message.Entity;
using Robin.Implementations.OneBot.Converter;

namespace Robin.Implementations.OneBot.Entity.Message.Data;

[Serializable]
[OneBotSegmentData("markdown", typeof(MarkdownData))]
internal class OneBotMarkdownData : IOneBotSegmentData
{
    [JsonPropertyName("content")] public required string Content { get; set; }
    public OneBotSegment FromSegmentData(SegmentData data, OneBotMessageConverter converter)
    {
        var d = data as MarkdownData;
        Content = JsonSerializer.Serialize(new { content = d!.Content });
        return new OneBotSegment
        {
            Type = "markdown",
            Data = JsonSerializer.SerializeToNode(this)
        };
    }

    public SegmentData ToSegmentData(OneBotMessageConverter converter) => throw new NotImplementedException();
}
