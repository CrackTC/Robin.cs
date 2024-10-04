using Microsoft.Extensions.Hosting;
using Robin.Abstractions.Context;
using Robin.Abstractions.Event;

namespace Robin.Abstractions;

public abstract class BotFunction(FunctionContext context) : IHostedService
{
    protected readonly FunctionContext _context = context;

    public virtual Task OnEventAsync(EventContext<BotEvent> eventContext) => Task.CompletedTask;

    public virtual Task StartAsync(CancellationToken token) => Task.CompletedTask;

    public virtual Task StopAsync(CancellationToken token) => Task.CompletedTask;
}
