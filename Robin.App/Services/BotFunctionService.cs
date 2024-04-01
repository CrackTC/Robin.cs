using System.Reflection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Robin.Abstractions;
using Robin.Abstractions.Event;

namespace Robin.App.Services;

// scoped, every bot has its own function, **DO NOT register it as IHostedService**
internal partial class BotFunctionService(
    ILogger<BotFunctionService> logger,
    IServiceProvider service,
    BotContext context) : IHostedService
{
    private readonly Dictionary<Type, List<BotFunction>> _eventToFunctions = [];
    private readonly List<BotFunction> _functions = [];

    private async Task RegisterFunctions(CancellationToken token)
    {
        var types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetExportedTypes())
            .Where(type => type.IsSubclassOf(typeof(BotFunction))
                           && type.GetCustomAttribute<BotFunctionInfoAttribute>() is not null);

        foreach (var type in types)
        {
            var info = type.GetCustomAttribute<BotFunctionInfoAttribute>()!;
            try
            {
                if (Activator.CreateInstance(
                        type,
                        service,
                        context.Uin,
                        context.OperationProvider,
                        context.FunctionConfigurations![info.Name],
                        _functions
                    ) is not BotFunction function)
                {
                    LogFunctionNotRegistered(logger, info.Name);
                    continue;
                }

                _functions.Add(function);

                foreach (var eventType in info.EventTypes)
                {
                    if (!eventType.IsSubclassOf(typeof(BotEvent)) && eventType != typeof(BotEvent)) continue;
                    if (_eventToFunctions.TryGetValue(eventType, out var functions))
                        functions.Add(function);
                    else
                        _eventToFunctions[eventType] = [function];
                }
            }
            catch (Exception e)
            {
                LogFunctionError(logger, info.Name, e);
            }
        }

        await Task.WhenAll(_functions.Select(function => function.StartAsync(token)));
    }

    private async Task OnBotEventAsync(BotEvent @event, CancellationToken token)
    {
        var type = @event.GetType();
        while (type != typeof(object))
        {
            if (_eventToFunctions.TryGetValue(type, out var functions))
                foreach (var function in functions)
                    await function.OnEventAsync(context.Uin, @event, token);

            type = type.BaseType!;
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (context.OperationProvider is null)
        {
            LogInvalidOption(logger, nameof(context.OperationProvider));
            return;
        }

        if (context.EventInvoker is null)
        {
            LogInvalidOption(logger, nameof(context.EventInvoker));
            return;
        }

        await RegisterFunctions(cancellationToken);
        context.EventInvoker.OnEventAsync += OnBotEventAsync;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        context.EventInvoker!.OnEventAsync -= OnBotEventAsync;
        foreach (var (_, functions) in _eventToFunctions)
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