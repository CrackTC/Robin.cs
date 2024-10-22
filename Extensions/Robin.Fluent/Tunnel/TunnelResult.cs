namespace Robin.Fluent.Tunnel;

internal interface ITunnelResult<out T>
{
    T? Data { get; }
    bool Accept { get; }
}

internal record TunnelResult<T>(T? Data, bool Accept) : ITunnelResult<T>;
