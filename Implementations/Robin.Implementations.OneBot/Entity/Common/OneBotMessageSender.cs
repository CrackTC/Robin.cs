using System.Text.Json.Serialization;
using Robin.Abstractions.Entity;

namespace Robin.Implementations.OneBot.Entity.Common;

internal class OneBotMessageSender
{
    [JsonPropertyName("user_id")]
    public long UserId { get; set; }

    [JsonPropertyName("nickname")]
    public required string Nickname { get; set; }

    [JsonPropertyName("sex")]
    public OneBotSex Sex { get; set; } = OneBotSex.Unknown;

    [JsonPropertyName("age")]
    public int Age { get; set; }

    public MessageSender ToMessageSender() => new(UserId, Nickname, Sex.ToUserSex(), Age);
}
