using System.Reflection;
using Robin.Abstractions.Context;
using Robin.Abstractions.Event;
using Robin.Middlewares.Fluent.Event;
using Robin.Middlewares.Fluent.Tunnel;

namespace Robin.Middlewares.Fluent;

public class FunctionBuilder
{
    private readonly IList<EventTunnelInfo> _tunnels = [];

    internal void AddTunnel(EventTunnelInfo tunnel) => _tunnels.Add(tunnel);

    internal IEnumerable<EventTunnelInfo> Build() => _tunnels;

    public EventTunnelBuilder<EventContext<TEvent>> On<TEvent>() where TEvent : BotEvent =>
        new EventTunnelBuilder<EventContext<TEvent>>(
            this,
            new TunnelBuilder<EventContext<BotEvent>, EventContext<TEvent>>(
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
