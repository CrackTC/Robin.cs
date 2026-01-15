using System.Text.Json.Serialization;

namespace Robin.Implementations.OneBot.Entity.Events.Notice;

[OneBotPostType("notice")]
internal abstract class OneBotNoticeEvent : OneBotEvent
{
    [JsonPropertyName("notice_type")]
    public required string NoticeType { get; set; }
}
