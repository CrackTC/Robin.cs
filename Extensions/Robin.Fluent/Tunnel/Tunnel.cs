namespace Robin.Fluent.Tunnel;

internal delegate ITunnelResult<TOut> Tunnel<in TIn, out TOut>(TIn input);
