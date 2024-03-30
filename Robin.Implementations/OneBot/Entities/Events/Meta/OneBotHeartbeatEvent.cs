using System.Text.Json.Serialization;
using Robin.Abstractions.Entities;
using Robin.Abstractions.Event;
using Robin.Abstractions.Event.Meta;
using Robin.Implementations.OneBot.Entities.Common;
using Robin.Implementations.OneBot.Converters;

namespace Robin.Implementations.OneBot.Entities.Events.Meta;

[Serializable]
[OneBotEventType("heartbeat")]
internal class OneBotHeartbeatEvent : OneBotMetaEvent
{
    [JsonPropertyName("status")] public required OneBotStatus Status { get; set; }
    [JsonPropertyName("interval")] public long Interval { get; set; }

    public override BotEvent ToBotEvent(OneBotMessageConverter _) =>
        new HeartbeatEvent(Time, new BotStatus(Status.Online, Status.Good), Interval);
}