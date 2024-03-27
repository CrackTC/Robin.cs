using System.Reflection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Robin.Abstractions;
using Robin.Abstractions.Event;

namespace Robin.Services;

// scoped, every bot has its own function, **DO NOT register it as IHostedService**
internal partial class BotFunctionService(
    ILogger<BotFunctionService> logger,
    BotContext option) : IHostedService
{
    private readonly Dictionary<Type, List<BotFunction>> _eventToFunctions = [];

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
                if (Activator.CreateInstance(type, option.OperationProvider) is not BotFunction function)
                {
                    LogFunctionNotRegistered(logger, info.Name);
                    continue;
                }

                await function.StartAsync(token);

                foreach (var eventType in info.EventTypes)
                {
                    if (eventType.IsSubclassOf(typeof(BotEvent)) || eventType == typeof(BotEvent))
                    {
                        if (_eventToFunctions.TryGetValue(eventType, out List<BotFunction>? functions))
                            functions.Add(function);
                        else
                            _eventToFunctions[eventType] = [function];
                    }
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
            if (_eventToFunctions.TryGetValue(type, out List<BotFunction>? functions))
                foreach (var function in functions)
                    function.OnEvent(option.Uin, @event);

            type = type.BaseType!;
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (option.OperationProvider is null)
        {
            LogInvalidOption(logger, nameof(option.OperationProvider));
            return;
        }
        if (option.EventInvoker is null)
        {
            LogInvalidOption(logger, nameof(option.EventInvoker));
            return;
        }

        await RegisterFunctions(cancellationToken);
        option.EventInvoker.OnEvent += OnBotEvent;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        option.EventInvoker!.OnEvent -= OnBotEvent;
        foreach (var (type, functions) in _eventToFunctions)
            foreach (var function in functions)
                await function.StopAsync(cancellationToken);
    }

    #region Log

    [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Function {Name} is enabled")]
    private static partial void LogFunctionEnabled(ILogger logger, string name);

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Function {Name} is disabled")]
    private static partial void LogFunctionDisabled(ILogger logger, string name);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Function {Name} is not registered")]
    private static partial void LogFunctionNotRegistered(ILogger logger, string name);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "Error while invoking function {Name}")]
    private static partial void LogFunctionError(ILogger logger, string name, Exception exception);

    [LoggerMessage(EventId = 4, Level = LogLevel.Warning, Message = "Invalid option {Name}")]
    private static partial void LogInvalidOption(ILogger logger, string name);

    #endregion
}