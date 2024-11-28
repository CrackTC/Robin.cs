using System.Reflection;
using Robin.Abstractions.Context;
using Robin.Abstractions.Event;
using Robin.Middlewares.Fluent.Event;
using Robin.Middlewares.Fluent.Tunnel;

namespace Robin.Middlewares.Fluent;

internal record FunctionInfo(IEnumerable<EventTunnel> EventTunnels);

public class FunctionBuilder
{
    private readonly IList<EventTunnel> _eventTunnels = [];

    internal void AddEventTunnel(EventTunnel tunnel) => _eventTunnels.Add(tunnel);

    internal FunctionInfo Build() => new(_eventTunnels);

    public EventTunnelBuilder<EventContext<TEvent>> On<TEvent>(string? name = null) where TEvent : BotEvent =>
        new EventTunnelBuilder<EventContext<TEvent>>(
            this,
            name,
            new Tunnel<EventContext<BotEvent>, EventContext<TEvent>>(
                ctx => ctx.Event switch
                {
                    TEvent e => new TunnelResult<EventContext<TEvent>>(new EventContext<TEvent>(e, ctx.Token), true),
                    _ => new TunnelResult<EventContext<TEvent>>(default, false)
                }
            )
        )
        .WithDescription($"收到{typeof(TEvent)
            .GetCustomAttribute<EventDescriptionAttribute>()?.Description ?? typeof(TEvent).Name}");
}
