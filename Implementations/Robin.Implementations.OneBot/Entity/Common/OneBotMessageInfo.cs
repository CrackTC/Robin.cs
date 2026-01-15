using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Robin.Abstractions.Entity;
using Robin.Implementations.OneBot.Converter;

namespace Robin.Implementations.OneBot.Entity.Common;

internal class OneBotMessageInfo
{
    [JsonPropertyName("time")]
    public int Time { get; set; }

    [JsonPropertyName("message_type")]
    public required OneBotMessageType MessageType { get; set; }

    [JsonPropertyName("message_id")]
    public int MessageId { get; set; }

    [JsonPropertyName("real_id")]
    public int RealId { get; set; }

    [JsonPropertyName("sender")]
    public required OneBotGroupMessageSender Sender { get; set; }

    [JsonPropertyName("message")]
    public required JsonNode Message { get; set; }

    public MessageInfo ToMessageInfo(OneBotMessageConverter converter) =>
        new(
            Time,
            MessageType.ToMessageType(),
            MessageId.ToString(),
            RealId.ToString(),
            Sender.ToMessageSender(),
            converter.ParseMessageChain(Message) ?? throw new()
        );
}
