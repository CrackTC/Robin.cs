using System.Text.Json.Serialization;

namespace Robin.Implementations.OneBot.Entity.Events.Request;

[OneBotPostType("request")]
internal abstract class OneBotRequestEvent : OneBotEvent
{
    [JsonPropertyName("request_type")]
    public required string RequestType { get; set; }

    [JsonPropertyName("comment")]
    public required string Comment { get; set; }

    [JsonPropertyName("flag")]
    public required string Flag { get; set; }
}
