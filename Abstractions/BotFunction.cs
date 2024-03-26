using Microsoft.Extensions.Hosting;
using Robin.Abstractions.Event;

namespace Robin.Abstractions;

public abstract class BotFunction : IHostedService
{
    public bool IsEnabled { get; internal set; }
    public abstract void OnEvent(long selfId, BotEvent @event);

    public abstract Task StartAsync(CancellationToken cancellationToken);

    public abstract Task StopAsync(CancellationToken cancellationToken);
}