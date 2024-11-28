using Robin.Middlewares.Fluent.Tunnel;

namespace Robin.Middlewares.Fluent.Event;

internal record FluentTunnel<TIn>(IEnumerable<string> Descriptions, Tunnel<TIn, Task> Tunnel);
