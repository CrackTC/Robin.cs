using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Robin.Abstractions.Event;

namespace Robin.Abstractions.Context;

public class FunctionContext(
    ILogger logger,
    BotContext botContext,
    IConfigurationSection? configuration,
    EventFilter groupFilter,
    EventFilter privateFilter
)
{
    public ILogger Logger { get; } = logger;
    public BotContext BotContext { get; } = botContext;
    public IConfigurationSection? Configuration { get; } = configuration;
    public EventFilter GroupFilter { get; } = groupFilter;
    public EventFilter PrivateFilter { get; } = privateFilter;
}

public class FunctionContext<TConfig>(
    ILogger logger,
    BotContext botContext,
    TConfig configuration,
    EventFilter groupFilter,
    EventFilter privateFilter
) : FunctionContext(logger, botContext, null, groupFilter, privateFilter)
{
    public new TConfig Configuration => configuration;
}
