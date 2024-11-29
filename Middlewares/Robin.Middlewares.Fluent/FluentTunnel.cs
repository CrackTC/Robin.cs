using Robin.Middlewares.Fluent.Tunnel;

namespace Robin.Middlewares.Fluent;

internal record FluentTunnel<TIn>(string? Name, IEnumerable<string> Descriptions, Tunnel<TIn, Task> Tunnel);
