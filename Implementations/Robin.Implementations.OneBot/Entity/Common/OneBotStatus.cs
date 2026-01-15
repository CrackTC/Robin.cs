using System.Text.Json.Serialization;
using Robin.Abstractions.Entity;

namespace Robin.Implementations.OneBot.Entity.Common;

internal class OneBotStatus
{
    [JsonPropertyName("online")] public bool Online { get; set; }
    [JsonPropertyName("good")] public bool Good { get; set; }

    public BotStatus ToBotStatus() => new(Online, Good);
}
