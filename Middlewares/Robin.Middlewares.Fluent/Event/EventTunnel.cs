using Robin.Abstractions.Context;
using Robin.Abstractions.Event;
using Robin.Middlewares.Fluent.Tunnel;

namespace Robin.Middlewares.Fluent.Event;

internal record EventTunnel(
    int Priority,
    string? Name,
    IEnumerable<string> Descriptions,
    Tunnel<EventContext<BotEvent>, Task> Tunnel
) : FluentTunnel<EventContext<BotEvent>>(Name, Descriptions, Tunnel);
