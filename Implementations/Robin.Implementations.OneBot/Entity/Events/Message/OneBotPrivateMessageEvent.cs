using System.Text.Json.Serialization;
using Robin.Abstractions.Entity;
using Robin.Abstractions.Event;
using Robin.Abstractions.Event.Message;
using Robin.Implementations.OneBot.Converter;
using Robin.Implementations.OneBot.Entity.Common;

namespace Robin.Implementations.OneBot.Entity.Events.Message;

[Serializable]
[OneBotEventType("private")]
internal class OneBotPrivateMessageEvent : OneBotMessageEvent
{
    [JsonPropertyName("sender")]
    public required OneBotMessageSender Sender { get; set; }

    public override BotEvent ToBotEvent(OneBotMessageConverter converter)
    {
        return new PrivateMessageEvent(
            Time,
            MessageId.ToString(),
            UserId,
            converter.ParseMessageChain(Message) ?? [],
            Font,
            new MessageSender(Sender.UserId, Sender.Nickname, Sender.Sex.ToUserSex(), Sender.Age)
        );
    }
}
