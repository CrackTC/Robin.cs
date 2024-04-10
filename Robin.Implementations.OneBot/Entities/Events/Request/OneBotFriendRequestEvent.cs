using System.Text.Json.Serialization;
using Robin.Abstractions.Event;
using Robin.Abstractions.Event.Request;
using Robin.Implementations.OneBot.Converters;

namespace Robin.Implementations.OneBot.Entities.Events.Request;

[Serializable]
[OneBotEventType("friend")]
internal class OneBotFriendRequestEvent : OneBotRequestEvent
{
    [JsonPropertyName("user_id")] public long UserId { get; set; }

    public override BotEvent ToBotEvent(OneBotMessageConverter converter) =>
        new FriendRequestEvent(Time, UserId, Comment, Flag);
}