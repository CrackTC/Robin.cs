namespace Robin.Middlewares.Fluent;

internal static class FluentTunnelExt
{
    public static IEnumerable<string> GetDescriptions<T>(this IEnumerable<FluentTunnel<T>> tunnels) =>
        tunnels.Select(GetDescription);

    public static string GetDescription<T>(this FluentTunnel<T> tunnel) =>
        (tunnel.Name is null ? string.Empty : tunnel.Name + ": ") + string.Join(" 且 ", tunnel.Descriptions);
}
