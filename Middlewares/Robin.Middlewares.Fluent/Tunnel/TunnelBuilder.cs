namespace Robin.Middlewares.Fluent.Tunnel;

internal class TunnelBuilder<TIn, TOut>(Tunnel<TIn, TOut> tunnel)
{
    public TunnelBuilder<TIn, TOut> Where(Predicate<TOut> predicate) =>
        new(input =>
        {
            var result = tunnel(input);
            if (!result.Accept) return new TunnelResult<TOut>(default, false);
            return new TunnelResult<TOut>(result.Data, predicate(result.Data!));
        });

    public TunnelBuilder<TIn, TNewOut> Select<TNewOut>(Func<TOut, TNewOut> selector) =>
        new(input =>
        {
            var result = tunnel(input);
            if (!result.Accept) return new TunnelResult<TNewOut>(default, false);
            return new TunnelResult<TNewOut>(selector(result.Data!), true);
        });

    public Tunnel<TIn, TOut> Tunnel => tunnel;
}
