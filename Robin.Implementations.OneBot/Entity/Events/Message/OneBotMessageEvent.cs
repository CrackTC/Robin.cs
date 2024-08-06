using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Robin.Implementations.OneBot.Entity.Events.Message;

[Serializable]
[OneBotPostType("message")]
internal abstract class OneBotMessageEvent : OneBotEvent
{
    [JsonPropertyName("message_type")] public required string MessageType { get; set; }
    [JsonPropertyName("message_id")] public int MessageId { get; set; }
    [JsonPropertyName("user_id")] public long UserId { get; set; }
    [JsonPropertyName("message")] public JsonNode? Message { get; set; }
    [JsonPropertyName("font")] public int Font { get; set; }
}