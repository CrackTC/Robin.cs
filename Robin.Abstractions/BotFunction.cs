using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Robin.Abstractions.Communication;
using Robin.Abstractions.Event;

namespace Robin.Abstractions;

public abstract class BotFunction(
    IServiceProvider service,
    long uin,
    IOperationProvider provider,
    IConfiguration configuration,
    IEnumerable<BotFunction> functions) : IHostedService
{
    protected readonly IServiceProvider _service = service;
    protected readonly long _uin = uin;
    protected readonly IOperationProvider _provider = provider;
    protected readonly IConfiguration _configuration = configuration;
    protected readonly IEnumerable<BotFunction> _functions = functions;

    public virtual Task OnEventAsync(long selfId, BotEvent @event, CancellationToken token) => Task.CompletedTask;

    public virtual Task StartAsync(CancellationToken token) => Task.CompletedTask;

    public virtual Task StopAsync(CancellationToken token) => Task.CompletedTask;
}