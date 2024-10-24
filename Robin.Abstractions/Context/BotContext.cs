using System.Collections.Frozen;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Robin.Abstractions.Communication;

namespace Robin.Abstractions.Context;

// scoped, every bot has its own option
public partial class BotContext(
    IServiceProvider serviceProvider,
    List<BotFunction> functions
) : IDisposable
{
    public long Uin { get; set; }
    public IBotEventInvoker? EventInvoker { get; set; }
    public IOperationProvider OperationProvider { get; set; } = null!;
    public IEnumerable<BotFunction> Functions => functions;
    public FrozenDictionary<string, IConfigurationSection>? FunctionConfigurations { get; set; }

    private Type? GetOptionType(Type functionType)
    {
        for (var t = functionType.BaseType; t != null; t = t.BaseType)
        {
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(BotFunction<>))
            {
                return t.GetGenericArguments()[0];
            }
        }

        return null;
    }

    public FunctionContext? CreateFunctionContext(string functionName, Type functionType)
    {
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(functionType);
        if (FunctionConfigurations?.TryGetValue(functionName, out var section) is true)
        {
            if (GetOptionType(functionType) is not { } configType)
                return new FunctionContext(logger, this, section);

            if (section.Get(configType) is not { } config)
            {
                LogOptionBindingFailed(logger);
                return null;
            }

            var funcCtxType = typeof(FunctionContext<>).MakeGenericType(configType);

            if (Activator.CreateInstance(funcCtxType, logger, this, config) is not FunctionContext funcCtx)
            {
                LogInstantiationFailed(logger);
                return null;
            }

            return funcCtx;
        }

        return new FunctionContext(logger, this, null);
    }

    public void Dispose()
    {
        EventInvoker?.Dispose();
        OperationProvider?.Dispose();

        GC.SuppressFinalize(this);
    }

    #region Log

    [LoggerMessage(Level = LogLevel.Warning, Message = "Option binding failed")]
    private static partial void LogOptionBindingFailed(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "FunctionContext instantiation failed")]
    private static partial void LogInstantiationFailed(ILogger logger);

    #endregion
}
