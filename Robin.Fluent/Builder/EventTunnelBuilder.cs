using Robin.Abstractions.Context;
using Robin.Abstractions.Event;

namespace Robin.Fluent.Builder;

public class EventTunnelBuilder<TEvent, TOut> where TEvent : BotEvent
{
    private readonly FunctionBuilder _funcBuilder;
    private readonly TunnelBuilder<EventContext<BotEvent>, TOut> _tunBuilder;
    private readonly int _priority;
    private readonly IEnumerable<string> _descriptions;

    internal EventTunnelBuilder(
        FunctionBuilder funcBuilder,
        TunnelBuilder<EventContext<BotEvent>, TOut> tunBuilder,
        int priority = 0,
        IEnumerable<string>? descriptions = null
    )
    {
        _funcBuilder = funcBuilder;
        _tunBuilder = tunBuilder;
        _priority = priority;
        _descriptions = descriptions ?? [];
    }

    private EventTunnelBuilder<TEvent, TNewOut> WithTunBuilder<TNewOut>(
        TunnelBuilder<EventContext<BotEvent>, TNewOut> tunBuilder
    ) => new(_funcBuilder, tunBuilder, _priority, _descriptions);

    public EventTunnelBuilder<TEvent, TOut> Where(Predicate<TOut> predicate) =>
        WithTunBuilder(_tunBuilder.Where(predicate));

    public EventTunnelBuilder<TEvent, TNewOut> Select<TNewOut>(Func<TOut, TNewOut> selector) =>
        WithTunBuilder(_tunBuilder.Select(selector));

    public EventTunnelBuilder<TEvent, TOut> WithPriority(int priority) =>
        new(_funcBuilder, _tunBuilder, priority, _descriptions);

    public EventTunnelBuilder<TEvent, TOut> AsFallback() => WithPriority(int.MaxValue);

    public EventTunnelBuilder<TEvent, TOut> AsAlwaysFired() => WithPriority(int.MinValue);

    internal EventTunnelBuilder<TEvent, TOut> WithDescription(string description) =>
        new(_funcBuilder, _tunBuilder, _priority, [.. _descriptions, description]);

    public FunctionBuilder Do(Func<TOut, Task> something)
    {
        _funcBuilder.AddTunnel(new TunnelInfo(
            _priority,
            _descriptions,
            new EventTunnel(_tunBuilder.Select(something).Tunnel)
        ));
        return _funcBuilder;
    }
}