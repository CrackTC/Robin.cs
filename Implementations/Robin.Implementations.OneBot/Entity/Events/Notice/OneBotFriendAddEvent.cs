using System.Text.Json.Serialization;
using Robin.Abstractions.Event;
using Robin.Abstractions.Event.Notice;
using Robin.Implementations.OneBot.Converter;

namespace Robin.Implementations.OneBot.Entity.Events.Notice;

[Serializable]
[OneBotEventType("friend_add")]
internal class OneBotFriendAddEvent : OneBotNoticeEvent
{
    [JsonPropertyName("user_id")] public long UserId { get; set; }

    public override BotEvent ToBotEvent(OneBotMessageConverter converter) =>
        new FriendAddEvent(Time, UserId);
}
