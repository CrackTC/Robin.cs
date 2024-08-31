using System.Text.Json.Serialization;
using Robin.Abstractions.Event;
using Robin.Abstractions.Event.Notice.Member.Increase;
using Robin.Implementations.OneBot.Converter;

namespace Robin.Implementations.OneBot.Entity.Events.Notice;

[Serializable]
[OneBotEventType("group_increase")]
internal class OneBotGroupIncreaseEvent : OneBotNoticeEvent
{
    [JsonPropertyName("sub_type")] public required string SubType { get; set; }
    [JsonPropertyName("group_id")] public long GroupId { get; set; }
    [JsonPropertyName("operator_id")] public long OperatorId { get; set; }
    [JsonPropertyName("user_id")] public long UserId { get; set; }

    public override BotEvent ToBotEvent(OneBotMessageConverter converter) =>
        SubType switch
        {
            "approve" => new GroupApproveEvent(Time, GroupId, OperatorId, UserId),
            _ => new GroupInviteEvent(Time, GroupId, OperatorId, UserId)
        };
}