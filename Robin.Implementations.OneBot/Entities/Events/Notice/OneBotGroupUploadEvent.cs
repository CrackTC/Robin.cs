using System.Text.Json.Serialization;
using Robin.Abstractions.Entities;
using Robin.Abstractions.Event;
using Robin.Abstractions.Event.Notice;
using Robin.Implementations.OneBot.Converters;
using Robin.Implementations.OneBot.Entities.Common;

namespace Robin.Implementations.OneBot.Entities.Events.Notice;

[Serializable]
[OneBotEventType("group_upload")]
internal class OneBotGroupUploadEvent : OneBotNoticeEvent
{
    [JsonPropertyName("group_id")] public long GroupId { get; set; }
    [JsonPropertyName("user_id")] public long UserId { get; set; }
    [JsonPropertyName("file")] public required OneBotUploadFile File { get; set; }

    public override BotEvent ToBotEvent(OneBotMessageConverter _) => new GroupUploadEvent(Time, GroupId, UserId,
        new UploadFile(File.Id, File.Name, File.Size, File.BusId));
}