using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Robin.Abstractions.Communication;
using Robin.Abstractions.Event;

namespace Robin.Abstractions;

public abstract class BotFunction(
    IServiceProvider service,
    long uin,
    IOperationProvider operation,
    IConfiguration configuration,
    IEnumerable<BotFunction> functions) : IHostedService
{
    protected readonly IServiceProvider _service = service;
    protected readonly long _uin = uin;
    protected readonly IOperationProvider _operation = operation;
    protected readonly IConfiguration _configuration = configuration;
    protected readonly IEnumerable<BotFunction> _functions = functions;

    public abstract Task OnEventAsync(long selfId, BotEvent @event, CancellationToken token);

    public abstract Task StartAsync(CancellationToken token);

    public abstract Task StopAsync(CancellationToken token);
}