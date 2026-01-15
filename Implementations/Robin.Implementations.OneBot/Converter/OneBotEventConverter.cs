using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Robin.Abstractions.Event;
using Robin.Implementations.OneBot.Entity.Events;

namespace Robin.Implementations.OneBot.Converter;

internal partial class OneBotEventConverter(ILogger<OneBotEventConverter> logger)
{
    public BotEvent? ParseBotEvent(JsonNode eventNode, OneBotMessageConverter converter)
    {
        if (OneBotEvent.GetEventType(eventNode) is not { } type)
        {
            LogInvalidEvent(logger, eventNode.ToJsonString());
            return null;
        }

        if (eventNode.Deserialize(type) is OneBotEvent @event)
            return @event.ToBotEvent(converter);

        LogInvalidEvent(logger, eventNode.ToJsonString());
        return null;
    }

    #region Log

    [LoggerMessage(Level = LogLevel.Warning, Message = "Invalid event: {Event}")]
    private static partial void LogInvalidEvent(ILogger logger, string @event);

    #endregion
}
