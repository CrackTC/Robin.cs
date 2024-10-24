using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Robin.Abstractions;
using Robin.Abstractions.Event;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Robin.Abstractions.Event.Meta;
using Robin.Abstractions.Context;

namespace Robin.App.Services;

// scoped, every bot has its own copy of functions, **DO NOT register it as IHostedService**
internal partial class BotFunctionService(
    ILogger<BotFunctionService> logger,
    BotContext context,
    List<BotFunction> functions,
    IEnumerable<Assembly> extensions
) : IHostedService
{
    private readonly Dictionary<Type, List<BotFunction>> _eventToFunctions = [];

    private async Task RegisterFunctions(CancellationToken token)
    {
        var types = extensions
            .SelectMany(assembly => assembly.GetExportedTypes())
            .Where(type => type.IsSubclassOf(typeof(BotFunction)));

        foreach (var type in types)
        {
            var info = type.GetCustomAttribute<BotFunctionInfoAttribute>()!;
            try
            {
                var instance = Activator.CreateInstance(
                    type,
                    context.CreateFunctionContext(info.Name, type)
                );

                if (instance is not BotFunction function)
                {
                    LogFunctionNotRegistered(logger, info.Name);
                    continue;
                }

                functions.Add(function);

                foreach (var eventType in info.EventTypes)
                {
                    if (!eventType.IsAssignableTo(typeof(BotEvent))) continue;
                    if (_eventToFunctions.TryGetValue(eventType, out var eventFunctions))
                        eventFunctions.Add(function);
                    else
                        _eventToFunctions[eventType] = [function];
                }
            }
            catch (Exception e)
            {
                LogCreateFunctionFailed(logger, info.Name, e);
            }
        }

        await Task.WhenAll(functions.Select(function => function.StartAsync(token)));
    }

    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
    };

    private async Task InvokeFunction(BotFunction function, EventContext<BotEvent> eventContext)
    {
        try
        {
            await function.OnEventAsync(eventContext);
        }
        catch (Exception e)
        {
            var name = function.GetType().GetCustomAttribute<BotFunctionInfoAttribute>()!.Name;
            LogInvokeFunctionFailed(logger, name, e);
        }
    }

    private Task OnBotEventAsync(BotEvent @event, CancellationToken token)
    {
        if (@event is not HeartbeatEvent)
        {
            LogReceivedEvent(
                logger,
                @event.GetType().Name,
                JsonSerializer.Serialize(
                    @event,
                    @event.GetType(),
                    _jsonSerializerOptions
                )
            );
        }

        var tasks = new List<Task>();
        var eventContext = new EventContext<BotEvent>(@event, token);

        for (var type = @event.GetType(); type.BaseType is not null; type = type.BaseType)
        {
            if (!_eventToFunctions.TryGetValue(type, out var eventFunctions)) continue;
            tasks.AddRange(eventFunctions.Select(function => InvokeFunction(function, eventContext)));
        }

        return Task.WhenAll(tasks);
    }

    public async Task StartAsync(CancellationToken token)
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

        await RegisterFunctions(token);
        context.EventInvoker.OnEventAsync += OnBotEventAsync;
    }

    public async Task StopAsync(CancellationToken token)
    {
        context.EventInvoker!.OnEventAsync -= OnBotEventAsync;
        await Task.WhenAll(functions.Select(function => function.StopAsync(token)));
    }

    #region Log

    [LoggerMessage(Level = LogLevel.Information, Message = "Function {Name} is not registered")]
    private static partial void LogFunctionNotRegistered(ILogger logger, string name);

    [LoggerMessage(Level = LogLevel.Information, Message = "Received {Type}: {Event}")]
    private static partial void LogReceivedEvent(ILogger logger, string type, string @event);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Error while creating function {Name}")]
    private static partial void LogCreateFunctionFailed(ILogger logger, string name, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Invalid option {Name}")]
    private static partial void LogInvalidOption(ILogger logger, string name);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Error while invoking function {Name}")]
    private static partial void LogInvokeFunctionFailed(ILogger logger, string name, Exception exception);

    #endregion
}
