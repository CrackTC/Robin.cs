using System.Text.Json.Serialization;
using Robin.Abstractions.Event;
using Robin.Abstractions.Event.Message;
using Robin.Implementations.OneBot.Converter;
using Robin.Implementations.OneBot.Entity.Common;

namespace Robin.Implementations.OneBot.Entity.Events.Message;

[OneBotEventType("group")]
internal class OneBotGroupMessageEvent : OneBotMessageEvent
{
    [JsonPropertyName("group_id")]
    public long GroupId { get; set; }

    [JsonPropertyName("anonymous")]
    public OneBotAnonymousInfo? Anonymous { get; set; }

    [JsonPropertyName("sender")]
    public required OneBotGroupMessageSender Sender { get; set; }

    public override BotEvent ToBotEvent(OneBotMessageConverter converter)
    {
        return new GroupMessageEvent(
            Time,
            MessageId.ToString(),
            GroupId,
            UserId,
            Anonymous?.ToAnonymousInfo(),
            converter.ParseMessageChain(Message) ?? [],
            Font,
            Sender.ToGroupMessageSender()
        );
    }
}
