namespace Robin.Middlewares.Fluent.Tunnel;

internal interface ITunnelResult<T>
{
    T? Data { get; }
    bool Accept { get; }
}

internal record TunnelResult<T>(T? Data, bool Accept) : ITunnelResult<T>;
