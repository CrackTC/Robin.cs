using System.Text.Json.Serialization;

namespace Robin.Implementations.OneBot.Entity.Common;

internal class OneBotMessageId
{
    [JsonPropertyName("message_id")] public int MessageId { get; set; }

    public string ToMessageId() => MessageId.ToString();
}