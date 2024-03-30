using Microsoft.Extensions.Hosting;
using Robin.Abstractions.Communication;
using Robin.Abstractions.Event;

namespace Robin.Abstractions;

public abstract class BotFunction(IServiceProvider service, IOperationProvider provider) : IHostedService
{
    protected readonly IServiceProvider _service = service;
    protected readonly IOperationProvider _provider = provider;

    public abstract void OnEvent(long selfId, BotEvent @event);

    public abstract Task StartAsync(CancellationToken cancellationToken);

    public abstract Task StopAsync(CancellationToken cancellationToken);
}