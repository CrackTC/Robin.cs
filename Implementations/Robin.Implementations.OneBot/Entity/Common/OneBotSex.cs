namespace Robin.Implementations.OneBot.Entity.Common;

using System.Text.Json.Serialization;
using Robin.Abstractions.Entity;

internal enum OneBotSex
{
    [JsonPropertyName("male")]
    Male,

    [JsonPropertyName("female")]
    Female,

    [JsonPropertyName("unknown")]
    Unknown,
}

internal static class OneBotSexExtensions
{
    public static UserSex ToUserSex(this OneBotSex sex) =>
        sex switch
        {
            OneBotSex.Male => UserSex.Male,
            OneBotSex.Female => UserSex.Female,
            _ => UserSex.Unknown,
        };
}
