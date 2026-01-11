namespace Robin.Middlewares.Fluent.Tunnel;

internal delegate Task<ITunnelResult<TOut>> Tunnel<in TIn, TOut>(TIn input);
