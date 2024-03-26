using System.Reflection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Robin.Abstractions;
using Robin.Abstractions.Communication;
using Robin.Abstractions.Event;

namespace Robin.Services;

public partial class BotFunctionService(
    ILogger<BotFunctionService> logger,
    BotLifetimeService lifetime,
    IOperationProvider provider,
    IBotEventInvoker invoker) : IHostedService
{
    private readonly Dictionary<Type, List<BotFunction>> _functions = [];

    private async Task RegisterFunctions(CancellationToken token)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var types = assemblies.SelectMany(assembly => assembly.GetExportedTypes())
            .Where(type => type.IsSubclassOf(typeof(BotFunction))
                && type.GetCustomAttribute<BotFunctionInfoAttribute>() is not null);

        foreach (var type in types)
        {
            var info = type.GetCustomAttribute<BotFunctionInfoAttribute>()!;
            try
            {
                if (Activator.CreateInstance(type, provider) is not BotFunction function)
                {
                    LogFunctionNotRegistered(logger, info.Name);
                    continue;
                }

                await function.StartAsync(token);

                if (info.EventType is { } eventType
                    && (eventType.IsSubclassOf(typeof(BotEvent))
                        || eventType == typeof(BotEvent)))
                {
                    if (!_functions.TryGetValue(eventType, out List<BotFunction>? value))
                    {
                        value = [];
                        _functions[eventType] = value;
                    }

                    value.Add(function);
                }
            }
            catch (Exception e)
            {
                LogFunctionError(logger, info.Name, e);
            }
        }
    }

    private void OnBotEvent(BotEvent @event)
    {
        var type = @event.GetType();
        while (type != typeof(object))
        {
            if (_functions.TryGetValue(type, out List<BotFunction>? functions))
                foreach (var function in functions)
                    if (function.IsEnabled) function.OnEvent(lifetime.Uin, @event);

            type = type.BaseType!;
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await RegisterFunctions(cancellationToken);
        invoker.OnEvent += OnBotEvent;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        invoker.OnEvent -= OnBotEvent;
        foreach (var (type, functions) in _functions)
            foreach (var function in functions)
                await function.StopAsync(cancellationToken);
    }

    [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Function {Name} is enabled")]
    private static partial void LogFunctionEnabled(ILogger logger, string name);

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Function {Name} is disabled")]
    private static partial void LogFunctionDisabled(ILogger logger, string name);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Function {Name} is not registered")]
    private static partial void LogFunctionNotRegistered(ILogger logger, string name);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "Error while invoking function {Name}")]
    private static partial void LogFunctionError(ILogger logger, string name, Exception exception);
}