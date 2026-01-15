using System.Text.Json.Serialization;
using Robin.Abstractions.Event;
using Robin.Abstractions.Event.Notice.Reaction;
using Robin.Implementations.OneBot.Converter;

namespace Robin.Implementations.OneBot.Entity.Events.Notice;

[OneBotEventType("reaction")]
internal class OneBotReactionEvent : OneBotNoticeEvent
{
    [JsonPropertyName("sub_type")]
    public required string SubType { get; set; }

    [JsonPropertyName("group_id")]
    public long GroupId { get; set; }

    [JsonPropertyName("message_id")]
    public long MessageId { get; set; }

    [JsonPropertyName("operator_id")]
    public long OperatorId { get; set; }

    [JsonPropertyName("code")]
    public required string Code { get; set; }

    [JsonPropertyName("count")]
    public required uint Count { get; set; }

    public override BotEvent ToBotEvent(OneBotMessageConverter converter) =>
        SubType switch
        {
            "add" => new ReactionAddEvent(
                Time,
                MessageId.ToString(),
                GroupId,
                OperatorId,
                Code,
                Count
            ),
            _ => new ReactionRemoveEvent(
                Time,
                MessageId.ToString(),
                GroupId,
                OperatorId,
                Code,
                Count
            ),
        };
}
