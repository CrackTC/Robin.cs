using System.Text.Json.Serialization;
using Robin.Abstractions.Event;
using Robin.Abstractions.Event.Request;
using Robin.Implementations.OneBot.Converter;

namespace Robin.Implementations.OneBot.Entity.Events.Request;

[Serializable]
[OneBotEventType("group")]
internal class OneBotGroupRequestEvent : OneBotRequestEvent
{
    [JsonPropertyName("sub_type")] public required string SubType { get; set; }
    [JsonPropertyName("group_id")] public long GroupId { get; set; }
    [JsonPropertyName("user_id")] public long UserId { get; set; }

    public override BotEvent ToBotEvent(OneBotMessageConverter converter) =>
        SubType switch
        {
            "add" => new GroupAddRequestEvent(Time, UserId, GroupId, Comment, Flag),
            _ => new GroupInviteRequestEvent(Time, UserId, GroupId, Comment, Flag)
        };
}