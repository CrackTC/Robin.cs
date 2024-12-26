using System.Text.Json.Serialization;
using Robin.Abstractions.Event;
using Robin.Abstractions.Event.Notice.Recall;
using Robin.Implementations.OneBot.Converter;

namespace Robin.Implementations.OneBot.Entity.Events.Notice;

[Serializable]
[OneBotEventType("group_recall")]
internal class OneBotGroupRecallEvent : OneBotNoticeEvent
{
    [JsonPropertyName("group_id")] public long GroupId { get; set; }
    [JsonPropertyName("user_id")] public long UserId { get; set; }
    [JsonPropertyName("operator_id")] public long OperatorId { get; set; }
    [JsonPropertyName("message_id")] public long MessageId { get; set; }

    public override BotEvent ToBotEvent(OneBotMessageConverter converter) =>
        new GroupRecallEvent(Time, UserId, MessageId.ToString(), GroupId, OperatorId);
}
