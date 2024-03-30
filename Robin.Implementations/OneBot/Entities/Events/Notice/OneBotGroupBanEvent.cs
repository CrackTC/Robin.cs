using System.Text.Json.Serialization;
using Robin.Abstractions.Event;
using Robin.Abstractions.Event.Notice.Ban;
using Robin.Implementations.OneBot.Converters;

namespace Robin.Implementations.OneBot.Entities.Events.Notice;

[Serializable]
[OneBotEventType("group_ban")]
internal class OneBotGroupBanEvent : OneBotNoticeEvent
{
    [JsonPropertyName("sub_type")] public required string SubType { get; set; }
    [JsonPropertyName("group_id")] public long GroupId { get; set; }
    [JsonPropertyName("operator_id")] public long OperatorId { get; set; }
    [JsonPropertyName("user_id")] public long UserId { get; set; }
    [JsonPropertyName("duration")] public long Duration { get; set; }

    public override BotEvent ToBotEvent(OneBotMessageConverter converter) =>
        SubType switch
        {
            "ban" => new GroupSetBanEvent(Time, GroupId, OperatorId, UserId, Duration),
            _ => new GroupUnsetBanEvent(Time, GroupId, OperatorId, UserId, Duration)
        };
}