using Robin.Middlewares.Fluent.Tunnel;

namespace Robin.Middlewares.Fluent.Cron;

internal record CronTunnel(
    string Cron,
    string Name,
    IEnumerable<string> Descriptions,
    Tunnel<CancellationToken, Task> Tunnel
) : FluentTunnel<CancellationToken>(Name, Descriptions, Tunnel);
