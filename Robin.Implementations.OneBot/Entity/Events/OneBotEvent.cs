using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Robin.Abstractions.Event;
using Robin.Implementations.OneBot.Converter;

namespace Robin.Implementations.OneBot.Entity.Events;

[Serializable]
internal abstract class OneBotEvent
{
    [JsonPropertyName("time")] public long Time { get; set; }
    [JsonPropertyName("self_id")] public long SelfId { get; set; }
    [JsonPropertyName("post_type")] public string PostType { get; set; } = string.Empty;

    public abstract BotEvent ToBotEvent(OneBotMessageConverter converter);

    private static readonly Dictionary<(string, string), Type> _eventTypeToType = [];

    static OneBotEvent()
    {
        var types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.BaseType?.IsAssignableTo(typeof(OneBotEvent)) ?? false)
            .Select(type => (Type: type, EventTypeAttribute: type.GetCustomAttribute<OneBotEventTypeAttribute>(),
                PostTypeAttribute: type.BaseType?.GetCustomAttribute<OneBotPostTypeAttribute>()))
            .Where(pair => pair.EventTypeAttribute is not null && pair.PostTypeAttribute is not null);

        foreach (var (type, eventTypeAttribute, postTypeAttribute) in types)
        {
            _eventTypeToType[(postTypeAttribute!.Type, eventTypeAttribute!.Type)] = type;
        }
    }

    public static Type? GetEventType(JsonNode node)
    {
        if (node["post_type"] is not { } postTypeNode) return null;
        if (postTypeNode.GetValueKind() != JsonValueKind.String) return null;
        var postType = postTypeNode.GetValue<string>();

        if (node[$"{postType}_type"] is not { } eventTypeNode) return null;
        if (eventTypeNode.GetValueKind() != JsonValueKind.String) return null;
        var eventType = eventTypeNode.GetValue<string>();

        return _eventTypeToType.GetValueOrDefault((postType, eventType));
    }
}
