using System.Text.Json.Serialization;
using Robin.Abstractions.Entity;

namespace Robin.Implementations.OneBot.Entity.Common;

internal class OneBotForwardResult
{
    [JsonPropertyName("message_id")]
    public required int MessageId { get; set; }

    [JsonPropertyName("res_id")]
    public required string ResId { get; set; }

    public ForwardResult ToForwardResult() => new(MessageId.ToString(), ResId);
}
