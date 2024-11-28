namespace Robin.Middlewares.Fluent.Tunnel;

internal static class TunnelExt
{
    public static Tunnel<TIn, TOut> Where<TIn, TOut>(this Tunnel<TIn, TOut> tunnel, Predicate<TOut> predicate) =>
        new(input =>
        {
            var result = tunnel(input);
            if (!result.Accept) return new TunnelResult<TOut>(default, false);
            return new TunnelResult<TOut>(result.Data, predicate(result.Data!));
        });

    public static Tunnel<TIn, TNewOut> Select<TIn, TOut, TNewOut>(this Tunnel<TIn, TOut> tunnel, Func<TOut, TNewOut> selector) =>
        new(input =>
        {
            var result = tunnel(input);
            if (!result.Accept) return new TunnelResult<TNewOut>(default, false);
            return new TunnelResult<TNewOut>(selector(result.Data!), true);
        });
}
