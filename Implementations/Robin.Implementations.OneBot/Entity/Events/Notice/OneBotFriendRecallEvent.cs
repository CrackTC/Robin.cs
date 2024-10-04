using System.Text.Json.Serialization;
using Robin.Abstractions.Event;
using Robin.Abstractions.Event.Notice.Recall;
using Robin.Implementations.OneBot.Converter;

namespace Robin.Implementations.OneBot.Entity.Events.Notice;

[Serializable]
[OneBotEventType("friend_recall")]
internal class OneBotFriendRecallEvent : OneBotNoticeEvent
{
    [JsonPropertyName("user_id")] public long UserId { get; set; }
    [JsonPropertyName("message_id")] public long MessageId { get; set; }

    public override BotEvent ToBotEvent(OneBotMessageConverter converter) =>
        new FriendRecallEvent(Time, UserId, MessageId);
}
