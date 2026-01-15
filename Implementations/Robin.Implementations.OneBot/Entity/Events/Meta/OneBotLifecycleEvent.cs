using System.Text.Json.Serialization;
using Robin.Abstractions.Event;
using Robin.Abstractions.Event.Meta.Lifetime;
using Robin.Implementations.OneBot.Converter;

namespace Robin.Implementations.OneBot.Entity.Events.Meta;

[OneBotEventType("lifecycle")]
internal class OneBotLifecycleEvent : OneBotMetaEvent
{
    [JsonPropertyName("sub_type")]
    public required string SubType { get; set; }

    public override BotEvent ToBotEvent(OneBotMessageConverter _) =>
        SubType switch
        {
            "enable" => new LifecycleEnableEvent(Time),
            "disable" => new LifecycleDisableEvent(Time),
            _ => new LifecycleConnectEvent(Time),
        };
}
