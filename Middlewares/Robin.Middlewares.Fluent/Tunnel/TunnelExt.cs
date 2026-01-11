namespace Robin.Middlewares.Fluent.Tunnel;

internal static class TunnelExt
{
    public static Tunnel<TIn, TOut> Where<TIn, TOut>(this Tunnel<TIn, TOut> tunnel, Predicate<TOut> predicate) =>
        new(async input =>
        {
            var result = await tunnel(input);
            if (!result.Accept) return new TunnelResult<TOut>(default, false);
            return new TunnelResult<TOut>(result.Data, predicate(result.Data!));
        });

    public static Tunnel<TIn, TOut> Where<TIn, TOut>(this Tunnel<TIn, TOut> tunnel, Func<TOut, Task<bool>> predicate) =>
        new(async input =>
        {
            var result = await tunnel(input);
            if (!result.Accept) return new TunnelResult<TOut>(default, false);
            return new TunnelResult<TOut>(result.Data, await predicate(result.Data!));
        });

    public static Tunnel<TIn, TNewOut> Select<TIn, TOut, TNewOut>(this Tunnel<TIn, TOut> tunnel, Func<TOut, TNewOut> selector) =>
        new(async input =>
        {
            var result = await tunnel(input);
            if (!result.Accept) return new TunnelResult<TNewOut>(default, false);
            return new TunnelResult<TNewOut>(selector(result.Data!), true);
        });

    public static Tunnel<TIn, TNewOut> Select<TIn, TOut, TNewOut>(this Tunnel<TIn, TOut> tunnel, Func<TOut, Task<TNewOut>> selector) =>
        new(async input =>
        {
            var result = await tunnel(input);
            if (!result.Accept) return new TunnelResult<TNewOut>(default, false);
            return new TunnelResult<TNewOut>(await selector(result.Data!), true);
        });
}
