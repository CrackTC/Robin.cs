using System.Text.Json.Serialization;
using Robin.Abstractions.Event;
using Robin.Abstractions.Event.Notice;
using Robin.Implementations.OneBot.Converter;
using Robin.Implementations.OneBot.Entity.Common;

namespace Robin.Implementations.OneBot.Entity.Events.Notice;

[Serializable]
[OneBotEventType("group_upload")]
internal class OneBotGroupUploadEvent : OneBotNoticeEvent
{
    [JsonPropertyName("group_id")] public long GroupId { get; set; }
    [JsonPropertyName("user_id")] public long UserId { get; set; }
    [JsonPropertyName("file")] public required OneBotUploadFile File { get; set; }

    public override BotEvent ToBotEvent(OneBotMessageConverter _) => new GroupUploadEvent(Time, GroupId, UserId, File.ToUploadFile());
}
