namespace Robin.Fluent.Builder;

internal class TunnelBuilder<TIn, TOut>(Func<TIn, TunnelResult<TOut>> tunnel)
{

    public TunnelBuilder<TIn, TOut> Where(Predicate<TOut> predicate) =>
        new(input =>
        {
            var (data, accept) = tunnel(input);
            if (!accept) return new TunnelResult<TOut>(default, false);
            return new TunnelResult<TOut>(data, predicate(data!));
        });

    public TunnelBuilder<TIn, TNewOut> Select<TNewOut>(Func<TOut, TNewOut> selector) =>
        new(input =>
        {
            var (data, accept) = tunnel(input);
            if (!accept) return new TunnelResult<TNewOut>(default, false);
            return new TunnelResult<TNewOut>(selector(data!), true);
        });

    public Func<TIn, TunnelResult<TOut>> Tunnel => tunnel;
}
