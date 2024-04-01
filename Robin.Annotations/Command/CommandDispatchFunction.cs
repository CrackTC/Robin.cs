using System.Collections.Frozen;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Robin.Abstractions;
using Robin.Abstractions.Communication;
using Robin.Abstractions.Event;
using Robin.Abstractions.Event.Message;
using Robin.Abstractions.Message.Entities;

// ReSharper disable PossibleMultipleEnumeration

namespace Robin.Annotations.Command;

[BotFunctionInfo("command_dispatch", "dispatch command to function", typeof(GroupMessageEvent))]
public class CommandDispatchFunction(
    IServiceProvider service,
    IOperationProvider operation,
    IConfiguration configuration,
    IEnumerable<BotFunction> functions) : BotFunction(service, operation, configuration, functions)
{
    private readonly FrozenDictionary<string, ICommandHandler> _functionMap = functions
        .Select(function =>
            (Function: function as ICommandHandler,
                Attribute: function.GetType().GetCustomAttribute<OnCommandAttribute>()))
        .Where(pair => pair.Attribute is not null && pair.Function is not null)
        .ToFrozenDictionary(pair => pair.Attribute!.Command, pair => pair.Function!);

    public override async Task OnEventAsync(long selfId, BotEvent @event, CancellationToken token)
    {
        var e = (@event as MessageEvent)!;

        if (e.Message.Segments.FirstOrDefault(segment => segment is TextData) is not TextData command) return;
        var commandText = command.Text.Trim().Split(' ').FirstOrDefault() ?? string.Empty;

        if (!commandText.StartsWith('/')) return;

        if (_functionMap.TryGetValue(commandText[1..], out var function))
        {
            await function.OnCommandAsync(selfId, e, token);
        }
    }

    public override Task StartAsync(CancellationToken token) => Task.CompletedTask;

    public override Task StopAsync(CancellationToken token) => Task.CompletedTask;
}