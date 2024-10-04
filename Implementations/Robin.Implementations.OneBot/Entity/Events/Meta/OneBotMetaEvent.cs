using System.Text.Json.Serialization;

namespace Robin.Implementations.OneBot.Entity.Events.Meta;

[Serializable]
[OneBotPostType("meta_event")]
internal abstract class OneBotMetaEvent : OneBotEvent
{
    [JsonPropertyName("meta_event_type")] public required string MetaEventType { get; set; }
}
