using System.Text.Json.Serialization;
using Robin.Abstractions.Event;
using Robin.Abstractions.Event.Notice;
using Robin.Abstractions.Event.Notice.Honor;
using Robin.Implementations.OneBot.Converters;

namespace Robin.Implementations.OneBot.Entities.Events.Notice;

[Serializable]
[OneBotEventType("notify")]
internal class OneBotNotifyEvent : OneBotNoticeEvent
{
    [JsonPropertyName("sub_type")] public required string SubType { get; set; }
    [JsonPropertyName("group_id")] public long GroupId { get; set; }
    [JsonPropertyName("user_id")] public long UserId { get; set; }
    [JsonPropertyName("target_id")] public long TargetId { get; set; }
    [JsonPropertyName("honor_type")] public required string HonorType { get; set; }

    public override BotEvent ToBotEvent(OneBotMessageConverter converter) =>
        SubType switch
        {
            "poke" => new GroupPokeEvent(Time, GroupId, UserId, TargetId),
            "lucky_king" => new LuckyKingEvent(Time, GroupId, UserId, TargetId),
            _ => HonorType switch
            {
                "talkative" => new GroupTalkativeEvent(Time, GroupId, UserId),
                "performer" => new GroupPerformerEvent(Time, GroupId, UserId),
                _ => new GroupEmotionEvent(Time, GroupId, UserId)
            }
        };
}