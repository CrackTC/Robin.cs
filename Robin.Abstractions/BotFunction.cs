using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Robin.Abstractions.Communication;
using Robin.Abstractions.Event;

namespace Robin.Abstractions;

public abstract class BotFunction(IServiceProvider service, IOperationProvider provider, IConfiguration configuration) : IHostedService
{
    protected readonly IServiceProvider _service = service;
    protected readonly IOperationProvider _provider = provider;
    protected readonly IConfiguration _configuration = configuration;

    public abstract void OnEvent(long selfId, BotEvent @event);

    public abstract Task StartAsync(CancellationToken token);

    public abstract Task StopAsync(CancellationToken token);
}