using System.Text.Json.Serialization;
using Robin.Abstractions.Event;
using Robin.Abstractions.Event.Notice.Member.Decrease;
using Robin.Implementations.OneBot.Converter;

namespace Robin.Implementations.OneBot.Entity.Events.Notice;

[OneBotEventType("group_decrease")]
internal class OneBotGroupDecreaseEvent : OneBotNoticeEvent
{
    [JsonPropertyName("sub_type")]
    public required string SubType { get; set; }

    [JsonPropertyName("group_id")]
    public long GroupId { get; set; }

    [JsonPropertyName("operator_id")]
    public long OperatorId { get; set; }

    [JsonPropertyName("user_id")]
    public long UserId { get; set; }

    public override BotEvent ToBotEvent(OneBotMessageConverter converter) =>
        SubType switch
        {
            "leave" => new GroupLeaveEvent(Time, GroupId, OperatorId, UserId),
            "kick" => new GroupKickEvent(Time, GroupId, OperatorId, UserId),
            _ => new GroupKickedEvent(Time, GroupId, OperatorId, UserId),
        };
}
