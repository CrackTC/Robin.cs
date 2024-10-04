using Robin.Abstractions.Event;

namespace Robin.Abstractions.Context;

public record EventContext<TEvent>(
    TEvent Event,
    CancellationToken Token
) where TEvent : BotEvent;
