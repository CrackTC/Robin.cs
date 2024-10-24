using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Robin.Abstractions.Context;

public class FunctionContext(
    ILogger logger,
    BotContext botContext,
    IConfigurationSection? configuration
)
{
    public ILogger Logger => logger;
    public BotContext BotContext => botContext;
    public IConfigurationSection? Configuration => configuration;
}

public class FunctionContext<TConfig>(
    ILogger logger,
    BotContext botContext,
    TConfig configuration
) : FunctionContext(logger, botContext, null)
{
    public new TConfig Configuration => configuration;
}
