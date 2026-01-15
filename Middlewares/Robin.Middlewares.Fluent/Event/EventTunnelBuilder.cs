using Robin.Abstractions.Context;
using Robin.Abstractions.Event;
using Robin.Middlewares.Fluent.Tunnel;

namespace Robin.Middlewares.Fluent.Event;

public class EventTunnelBuilder<TOut>
{
    private readonly FunctionBuilder _functionBuilder;
    private readonly string? _name;
    private readonly Tunnel<EventContext<BotEvent>, TOut> _tunnel;
    private readonly int _priority;
    private readonly IEnumerable<string> _descriptions;

    internal EventTunnelBuilder(
        FunctionBuilder functionBuilder,
        string? name,
        Tunnel<EventContext<BotEvent>, TOut> tunnel,
        int priority = 0,
        IEnumerable<string>? descriptions = null
    )
    {
        _functionBuilder = functionBuilder;
        _name = name;
        _tunnel = tunnel;
        _priority = priority;
        _descriptions = descriptions ?? [];
    }

    public EventTunnelBuilder<TOut> Where(Predicate<TOut> predicate) =>
        new(_functionBuilder, _name, _tunnel.Where(predicate), _priority, _descriptions);

    public EventTunnelBuilder<TOut> Where(Func<TOut, Task<bool>> predicate) =>
        new(_functionBuilder, _name, _tunnel.Where(predicate), _priority, _descriptions);

    public EventTunnelBuilder<TNewOut> Select<TNewOut>(Func<TOut, TNewOut> selector) =>
        new(_functionBuilder, _name, _tunnel.Select(selector), _priority, _descriptions);

    public EventTunnelBuilder<TNewOut> Select<TNewOut>(Func<TOut, Task<TNewOut>> selector) =>
        new(_functionBuilder, _name, _tunnel.Select(selector), _priority, _descriptions);

    public EventTunnelBuilder<TOut> WithPriority(int priority) =>
        new(_functionBuilder, _name, _tunnel, priority, _descriptions);

    public EventTunnelBuilder<TOut> AsFallback() =>
        new(_functionBuilder, _name, _tunnel, int.MaxValue, _descriptions.Append("未触发其他功能"));

    public EventTunnelBuilder<TOut> AsIntrinsic() =>
        new(_functionBuilder, _name, _tunnel, int.MinValue, _descriptions.Append("始终触发"));

    internal EventTunnelBuilder<TOut> WithDescription(string description) =>
        new(_functionBuilder, _name, _tunnel, _priority, _descriptions.Append(description));

    public FunctionBuilder Do(Func<TOut, Task> something)
    {
        _functionBuilder.AddEventTunnel(
            new EventTunnel(_priority, _name, _descriptions, _tunnel.Select(something))
        );
        return _functionBuilder;
    }
}
