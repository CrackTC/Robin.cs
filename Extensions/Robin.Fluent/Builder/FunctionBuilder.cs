using System.Reflection;
using Robin.Abstractions.Context;
using Robin.Abstractions.Event;

namespace Robin.Fluent.Builder;

internal delegate TunnelResult<Task> EventTunnel(EventContext<BotEvent> eventContext);

internal record TunnelInfo(int Priority, IEnumerable<string> Descriptions, EventTunnel Tunnel);


public class FunctionBuilder
{
    private readonly IList<TunnelInfo> _tunnels = [];

    internal void AddTunnel(TunnelInfo tunnel)
    {
        _tunnels.Add(tunnel);
    }

    internal IEnumerable<TunnelInfo> Build() => _tunnels;

    public EventTunnelBuilder<TEvent, EventContext<TEvent>> On<TEvent>() where TEvent : BotEvent =>
        new EventTunnelBuilder<TEvent, EventContext<TEvent>>(
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
