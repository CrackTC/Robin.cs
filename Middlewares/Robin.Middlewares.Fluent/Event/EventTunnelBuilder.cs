using Robin.Abstractions.Context;
using Robin.Abstractions.Event;
using Robin.Middlewares.Fluent.Tunnel;

namespace Robin.Middlewares.Fluent.Event;

internal record EventTunnelInfo(int Priority, IEnumerable<string> Descriptions, Tunnel<EventContext<BotEvent>, Task> Tunnel);

public class EventTunnelBuilder<TOut>
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

    private EventTunnelBuilder<TNewOut> WithTunBuilder<TNewOut>(
        TunnelBuilder<EventContext<BotEvent>, TNewOut> tunBuilder
    ) => new(_funcBuilder, tunBuilder, _priority, _descriptions);

    public EventTunnelBuilder<TOut> Where(Predicate<TOut> predicate) =>
        WithTunBuilder(_tunBuilder.Where(predicate));

    public EventTunnelBuilder<TNewOut> Select<TNewOut>(Func<TOut, TNewOut> selector) =>
        WithTunBuilder(_tunBuilder.Select(selector));

    public EventTunnelBuilder<TOut> WithPriority(int priority) =>
        new(_funcBuilder, _tunBuilder, priority, _descriptions);

    public EventTunnelBuilder<TOut> AsFallback() =>
        new(_funcBuilder, _tunBuilder, int.MaxValue, [.. _descriptions, "未触发其它功能"]);

    public EventTunnelBuilder<TOut> AsAlwaysFired() =>
        new(_funcBuilder, _tunBuilder, int.MinValue, [.. _descriptions, "始终触发"]);

    internal EventTunnelBuilder<TOut> WithDescription(string description) =>
        new(_funcBuilder, _tunBuilder, _priority, [.. _descriptions, description]);

    public FunctionBuilder Do(Func<TOut, Task> something)
    {
        _funcBuilder.AddTunnel(new EventTunnelInfo(
            _priority,
            _descriptions,
            _tunBuilder.Select(something).Tunnel
        ));
        return _funcBuilder;
    }
}
