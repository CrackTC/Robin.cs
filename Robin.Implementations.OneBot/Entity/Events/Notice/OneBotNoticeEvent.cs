using System.Text.Json.Serialization;

namespace Robin.Implementations.OneBot.Entity.Events.Notice;

[Serializable]
[OneBotPostType("notice")]
internal abstract class OneBotNoticeEvent : OneBotEvent
{
    [JsonPropertyName("notice_type")] public required string NoticeType { get; set; }
}