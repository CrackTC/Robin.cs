using System.Text.Json.Serialization;
using Robin.Abstractions.Entities;
using Robin.Abstractions.Event;
using Robin.Abstractions.Event.Message;
using Robin.Implementations.OneBot.Entities.Common;
using Robin.Implementations.OneBot.Converters;

namespace Robin.Implementations.OneBot.Entities.Events.Message;

[Serializable]
[OneBotEventType("private")]
internal class OneBotPrivateMessageEvent : OneBotMessageEvent
{
    [JsonPropertyName("sender")] public required OneBotMessageSender Sender { get; set; }

    public override BotEvent ToBotEvent(OneBotMessageConverter converter)
    {
        return new PrivateMessageEvent(Time, MessageId, UserId,
            converter.ParseMessageChain(Message) ?? [], Font,
            new MessageSender(Sender.UserId, Sender.Nickname,
                Sender.Sex switch { "male" => UserSex.Male, "female" => UserSex.Female, _ => UserSex.Unknown },
                Sender.Age));
    }
}