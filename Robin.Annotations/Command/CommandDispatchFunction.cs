using Microsoft.Extensions.Configuration;
using Robin.Abstractions;
using Robin.Abstractions.Communication;
using Robin.Abstractions.Event;
using Robin.Abstractions.Event.Message;
using Robin.Abstractions.Message.Entities;
using System.Collections.Frozen;
using System.Reflection;

// ReSharper disable PossibleMultipleEnumeration

namespace Robin.Annotations.Command;

[BotFunctionInfo("command_dispatch", "dispatch command to function", typeof(MessageEvent))]
public class CommandDispatchFunction(
    IServiceProvider service,
    long uin,
    IOperationProvider operation,
    IConfiguration configuration,
    IEnumerable<BotFunction> functions) : BotFunction(service, uin, operation, configuration, functions)
{
    private FrozenDictionary<string, (bool, ICommandHandler)>? _functionMap;

    public override async Task OnEventAsync(long selfId, BotEvent @event, CancellationToken token)
    {
        var e = (@event as MessageEvent)!;

        if (e.Message.FirstOrDefault(segment => segment is TextData data && !string.IsNullOrWhiteSpace(data.Text)) is not TextData command) return;
        var commandText = command.Text.Trim().Split(' ').FirstOrDefault() ?? string.Empty;

        if (!commandText.StartsWith('/') || !_functionMap!.ContainsKey(commandText[1..]))
            commandText = "/"; // try to match the default command

        if (_functionMap!.TryGetValue(commandText[1..], out var pair))
        {
            if (pair.Item1 && !e.Message.Any(segment => segment is AtData at && at.Uin == selfId))
                return;
            await pair.Item2.OnCommandAsync(selfId, e, token);
        }
    }

    public override Task StartAsync(CancellationToken token)
    {
        _functionMap = _functions
            .Select(function =>
                (Function: function as ICommandHandler,
                    Attributes: function.GetType().GetCustomAttributes<OnCommandAttribute>()))
            .Where(pair => pair.Attributes.Any() && pair.Function is not null)
            .SelectMany(pair => pair.Attributes.Select(attr => (pair.Function, attr.Command, attr.At)))
            .ToFrozenDictionary(tuple => tuple.Command, tuple => (tuple.At, tuple.Function!));
        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken token) => Task.CompletedTask;
}