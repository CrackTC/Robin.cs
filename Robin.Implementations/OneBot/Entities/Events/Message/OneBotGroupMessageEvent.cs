using System.Text.Json.Serialization;
using Robin.Abstractions.Entities;
using Robin.Abstractions.Event;
using Robin.Abstractions.Event.Message;
using Robin.Implementations.OneBot.Entities.Common;
using Robin.Implementations.OneBot.Converters;

namespace Robin.Implementations.OneBot.Entities.Events.Message;

[Serializable]
[OneBotEventType("group")]
internal class OneBotGroupMessageEvent : OneBotMessageEvent
{
    [JsonPropertyName("group_id")] public long GroupId { get; set; }
    [JsonPropertyName("anonymous")] public required OneBotAnonymousInfo Anonymous { get; set; }
    [JsonPropertyName("sender")] public required OneBotGroupMessageSender Sender { get; set; }

    public override BotEvent ToBotEvent(OneBotMessageConverter converter)
    {
        return new GroupMessageEvent(Time, MessageId, GroupId, UserId,
            new AnonymousInfo(Anonymous.Id, Anonymous.Name, Anonymous.Flag),
            converter.ParseMessageChain(Message) ?? [],
            Font,
            new GroupMessageSender(Sender.UserId, Sender.Nickname, Sender.Card,
                Sender.Sex switch { "male" => UserSex.Male, "female" => UserSex.Female, _ => UserSex.Unknown },
                Sender.Age, Sender.Area, Sender.Level,
                Sender.Role switch
                {
                    "owner" => GroupMemberRole.Owner,
                    "admin" => GroupMemberRole.Admin,
                    _ => GroupMemberRole.Member
                }, Sender.Title));
    }
}