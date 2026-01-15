using System.Text.Json.Serialization;
using Robin.Abstractions.Entity;

namespace Robin.Implementations.OneBot.Entity.Common;

internal enum OneBotMessageType
{
    [JsonPropertyName("private")] Private,
    [JsonPropertyName("group")] Group,
    [JsonPropertyName("temp")] Temp
}

internal static class OneBotMessageTypeExtensions
{
    public static MessageType ToMessageType(this OneBotMessageType messageType) => messageType switch
    {
        OneBotMessageType.Private => MessageType.Friend,
        OneBotMessageType.Group => MessageType.Group,
        OneBotMessageType.Temp => MessageType.Temp,
        _ => throw new ArgumentOutOfRangeException(nameof(messageType), messageType, null)
    };
}