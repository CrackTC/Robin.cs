using Robin.Middlewares.Fluent.Tunnel;

namespace Robin.Middlewares.Fluent.Cron;

public class CronTunnelBuilder<TOut>
{
    private readonly FunctionBuilder _functionBuilder;
    private readonly string _cron;
    private readonly string _name;
    private readonly Tunnel<CancellationToken, TOut> _tunnel;
    private readonly IEnumerable<string> _descriptions;

    internal CronTunnelBuilder(
        FunctionBuilder functionBuilder,
        string cron,
        string name,
        Tunnel<CancellationToken, TOut> tunnel,
        IEnumerable<string>? descriptions = null
    )
    {
        _functionBuilder = functionBuilder;
        _cron = cron;
        _name = name;
        _tunnel = tunnel;
        _descriptions = descriptions ?? [];
    }

    public CronTunnelBuilder<TOut> Where(Predicate<TOut> predicate) =>
        new(_functionBuilder, _cron, _name, _tunnel.Where(predicate), _descriptions);

    public CronTunnelBuilder<TNewOut> Select<TNewOut>(Func<TOut, TNewOut> selector) =>
        new(_functionBuilder, _cron, _name, _tunnel.Select(selector), _descriptions);

    internal CronTunnelBuilder<TOut> WithDescription(string description) =>
        new(_functionBuilder, _cron, _name, _tunnel, _descriptions.Append(description));

    public FunctionBuilder Do(Func<TOut, Task> something)
    {
        _functionBuilder.AddCronTunnel(
            new CronTunnel(_cron, _name, _descriptions, _tunnel.Select(something))
        );
        return _functionBuilder;
    }
}
