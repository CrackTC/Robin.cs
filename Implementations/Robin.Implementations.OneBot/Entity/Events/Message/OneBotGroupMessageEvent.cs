using System.Text.Json.Serialization;
using Robin.Abstractions.Entity;
using Robin.Abstractions.Event;
using Robin.Abstractions.Event.Message;
using Robin.Implementations.OneBot.Converter;
using Robin.Implementations.OneBot.Entity.Common;

namespace Robin.Implementations.OneBot.Entity.Events.Message;

[Serializable]
[OneBotEventType("group")]
internal class OneBotGroupMessageEvent : OneBotMessageEvent
{
    [JsonPropertyName("group_id")] public long GroupId { get; set; }
    [JsonPropertyName("anonymous")] public OneBotAnonymousInfo? Anonymous { get; set; }
    [JsonPropertyName("sender")] public required OneBotGroupMessageSender Sender { get; set; }

    public override BotEvent ToBotEvent(OneBotMessageConverter converter)
    {
        return new GroupMessageEvent(Time, MessageId.ToString(), GroupId, UserId,
            Anonymous is not null ? new AnonymousInfo(Anonymous.Id, Anonymous.Name, Anonymous.Flag) : null,
            converter.ParseMessageChain(Message) ?? [],
            Font,
            new GroupMessageSender(Sender.UserId, Sender.Nickname, Sender.Card,
                Sender.Sex switch { "male" => UserSex.Male, "female" => UserSex.Female, _ => UserSex.Unknown },
                Sender.Age, Sender.Area, Sender.Level is not null ? Convert.ToInt32(Sender.Level) : null,
                Sender.Role switch
                {
                    "owner" => GroupMemberRole.Owner,
                    "admin" => GroupMemberRole.Admin,
                    _ => GroupMemberRole.Member
                }, Sender.Title));
    }
}
