using System.Text.Json;
using System.Text.Json.Serialization;
using Robin.Abstractions.Message;
using Robin.Abstractions.Message.Entity;
using Robin.Implementations.OneBot.Converter;

namespace Robin.Implementations.OneBot.Entity.Message.Data;

[Serializable]
[OneBotSegmentData("mface", typeof(MarketFaceData))]
internal class OneBotMarketFaceData : IOneBotSegmentData
{
    [JsonPropertyName("url")] public required string Url { get; set; }
    [JsonPropertyName("emoji_id")] public required string EmojiId { get; set; }
    [JsonPropertyName("emoji_package_id")] public int EmojiPackageId { get; set; }
    [JsonPropertyName("key")] public required string Key { get; set; }
    [JsonPropertyName("summary")] public string? Summary { get; set; }

    public OneBotSegment FromSegmentData(SegmentData data, OneBotMessageConverter converter)
    {
        var d = data as MarketFaceData;
        Url = d!.Url;
        EmojiId = d.EmojiId;
        EmojiPackageId = d.EmojiPackageId;
        Key = d.Key;
        Summary = d.Summary;
        return new OneBotSegment { Type = "mface", Data = JsonSerializer.SerializeToNode(this) };
    }

    public SegmentData ToSegmentData(OneBotMessageConverter converter) =>
        new MarketFaceData(Url, EmojiId, EmojiPackageId, Key, Summary);
}
