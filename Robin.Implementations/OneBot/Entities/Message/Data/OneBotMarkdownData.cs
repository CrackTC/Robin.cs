﻿using Robin.Abstractions.Message;
using Robin.Abstractions.Message.Entities;
using Robin.Implementations.OneBot.Converters;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Robin.Implementations.OneBot.Entities.Message.Data;

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
