using System.Text.Json.Serialization;
using Robin.Abstractions.Event;
using Robin.Abstractions.Event.Notice.Admin;
using Robin.Implementations.OneBot.Converter;

namespace Robin.Implementations.OneBot.Entity.Events.Notice;

[Serializable]
[OneBotEventType("group_admin")]
internal class OneBotGroupAdminEvent : OneBotNoticeEvent
{
    [JsonPropertyName("sub_type")] public required string SubType { get; set; }
    [JsonPropertyName("group_id")] public long GroupId { get; set; }
    [JsonPropertyName("user_id")] public long UserId { get; set; }

    public override BotEvent ToBotEvent(OneBotMessageConverter converter) =>
        SubType switch
        {
            "set" => new GroupAdminSetEvent(Time, GroupId, UserId),
            _ => new GroupAdminUnsetEvent(Time, GroupId, UserId)
        };
}
