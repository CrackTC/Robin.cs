using System.Text.Json.Serialization;
using Robin.Abstractions.Entity;

namespace Robin.Implementations.OneBot.Entity.Common;

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum OneBotMessageType
{
    [JsonStringEnumMemberName("private")]
    Private,

    [JsonStringEnumMemberName("group")]
    Group,

    [JsonStringEnumMemberName("temp")]
    Temp,
}

internal static class OneBotMessageTypeExtensions
{
    public static MessageType ToMessageType(this OneBotMessageType messageType) =>
        messageType switch
        {
            OneBotMessageType.Private => MessageType.Friend,
            OneBotMessageType.Group => MessageType.Group,
            OneBotMessageType.Temp => MessageType.Temp,
            _ => throw new ArgumentOutOfRangeException(nameof(messageType), messageType, null),
        };
}
