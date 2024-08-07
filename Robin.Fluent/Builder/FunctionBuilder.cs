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
        new(
            this,
            new TunnelBuilder<EventContext<BotEvent>, EventContext<TEvent>>(
                eventContext =>
                    eventContext.Event is TEvent e
                        ? new(new EventContext<TEvent>(eventContext.Uin, e, eventContext.Token), true)
                        : new(default, false)
            )
        );
}