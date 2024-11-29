using System.Collections.Frozen;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Robin.Abstractions.Communication;
using Robin.Abstractions.Event;

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
    public FrozenDictionary<string, IConfigurationSection>? FilterConfigurations { get; set; }

    private static Type? GetOptionType(Type functionType)
    {
        for (var t = functionType.BaseType; t != null; t = t.BaseType)
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(BotFunction<>))
                return t.GetGenericArguments()[0];
        return null;
    }

    private static EventFilter GetEventFilter(IConfigurationSection filterSection)
    {
        bool whitelist = filterSection.GetValue<bool?>("Whitelist") ?? false;
        IEnumerable<long> ids = filterSection.GetSection("Ids").Get<List<long>>() ?? [];
        return new(ids, whitelist);
    }

    public FunctionContext? CreateFunctionContext(string functionName, Type functionType)
    {
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(functionType);

        var groupFilter = new EventFilter([]);
        var privateFilter = new EventFilter([]);

        if (FilterConfigurations?.TryGetValue(functionName, out var section) is true)
        {
            groupFilter = GetEventFilter(section.GetSection("Group"));
            privateFilter = GetEventFilter(section.GetSection("Private"));
        }

        // no configuration
        if (FunctionConfigurations?.TryGetValue(functionName, out section) is not true)
            return new FunctionContext(logger, this, null, groupFilter, privateFilter);

        // non-generic BotFunction, pass IConfigurationSection directly
        if (GetOptionType(functionType) is not { } configType)
            return new FunctionContext(logger, this, section, groupFilter, privateFilter);

        // generic BotFunction<>, bind configuration, instantiate FunctionContext<>
        if (section.Get(configType) is not { } config)
        {
            LogOptionBindingFailed(logger);
            return null;
        }

        var funcCtxType = typeof(FunctionContext<>).MakeGenericType(configType);
        if (Activator.CreateInstance(funcCtxType, logger, this, config, groupFilter, privateFilter)
                is not FunctionContext funcCtx)
        {
            LogInstantiationFailed(logger);
            return null;
        }

        return funcCtx;
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
