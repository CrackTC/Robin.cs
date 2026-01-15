using System.Text.Json.Serialization;
using Robin.Abstractions.Event;
using Robin.Abstractions.Event.Request;
using Robin.Implementations.OneBot.Converter;

namespace Robin.Implementations.OneBot.Entity.Events.Request;

[OneBotEventType("friend")]
internal class OneBotFriendRequestEvent : OneBotRequestEvent
{
    [JsonPropertyName("user_id")]
    public long UserId { get; set; }

    public override BotEvent ToBotEvent(OneBotMessageConverter converter) =>
        new FriendRequestEvent(Time, UserId, Comment, Flag);
}
