using System.Text.Json.Serialization;
using Robin.Abstractions.Entity;
using Robin.Abstractions.Event;
using Robin.Abstractions.Event.Meta;
using Robin.Implementations.OneBot.Converter;
using Robin.Implementations.OneBot.Entity.Common;

namespace Robin.Implementations.OneBot.Entity.Events.Meta;

[Serializable]
[OneBotEventType("heartbeat")]
internal class OneBotHeartbeatEvent : OneBotMetaEvent
{
    [JsonPropertyName("status")] public required OneBotStatus Status { get; set; }
    [JsonPropertyName("interval")] public long Interval { get; set; }

    public override BotEvent ToBotEvent(OneBotMessageConverter _) =>
        new HeartbeatEvent(Time, new BotStatus(Status.Online, Status.Good), Interval);
}
