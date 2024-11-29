using Microsoft.Extensions.Hosting;
using Robin.Abstractions.Context;
using Robin.Abstractions.Event;

namespace Robin.Abstractions;

public abstract class BotFunction(FunctionContext context) : IHostedService
{
    protected readonly FunctionContext _context = context;
    public FunctionContext Context => _context;

    public List<string> TriggerDescriptions { get; } = [];

    public virtual Task OnEventAsync(EventContext<BotEvent> eventContext) => Task.CompletedTask;

    public virtual Task StartAsync(CancellationToken token) => Task.CompletedTask;

    public virtual Task StopAsync(CancellationToken token) => Task.CompletedTask;
}

public abstract class BotFunction<TConfig>(FunctionContext<TConfig> context) : BotFunction(context)
{
    protected readonly new FunctionContext<TConfig> _context = context;
}
