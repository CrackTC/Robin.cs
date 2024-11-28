using Robin.Abstractions.Context;
using Robin.Abstractions.Event;
using Robin.Middlewares.Fluent.Tunnel;

namespace Robin.Middlewares.Fluent.Event;

internal record EventTunnel(
    int Priority,
    IEnumerable<string> Descriptions,
    Tunnel<EventContext<BotEvent>, Task> Tunnel
) : FluentTunnel<EventContext<BotEvent>>(Descriptions, Tunnel);
