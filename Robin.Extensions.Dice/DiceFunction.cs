using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Robin.Abstractions;
using Robin.Abstractions.Communication;
using Robin.Abstractions.Event;
using Robin.Abstractions.Event.Message;
using Robin.Abstractions.Message;
using Robin.Abstractions.Message.Entities;
using Robin.Abstractions.Operation.Requests;
using Robin.Annotations.Command;

namespace Robin.Extensions.Dice;

[BotFunctionInfo("dice", "Roll a dice.", typeof(GroupMessageEvent))]
[OnCommand("dice")]
// ReSharper disable once UnusedType.Global
public partial class DiceFunction(
    IServiceProvider service,
    long uin,
    IOperationProvider operation,
    IConfiguration configuration,
    IEnumerable<BotFunction> functions)
    : BotFunction(service, uin, operation, configuration, functions), ICommandHandler
{
    public override async Task OnEventAsync(long selfId, BotEvent @event, CancellationToken token)
    {
        if (@event is not MessageEvent e) return;
        if (e.Message.OfType<TextData>().FirstOrDefault()?.Text.Trim().StartsWith('/') ?? true) return;
        await OnCommandAsync(selfId, e, token);
    }

    public override Task StartAsync(CancellationToken token) => Task.CompletedTask;

    public override Task StopAsync(CancellationToken token) => Task.CompletedTask;

    public async Task OnCommandAsync(long selfId, MessageEvent @event, CancellationToken token)
    {
        if (@event is not GroupMessageEvent e) return;

        var text = string.Join("", e.Message
            .OfType<TextData>()
            .Select(data => data.Text.Trim())).Trim();

        if (string.IsNullOrEmpty(text)) return;

        var match = DiceRegex().Match(text);
        if (!match.Success) return;

        var count = match.Groups[1].Success ? int.Parse(match.Groups[1].Value) : 1;
        var sides = int.Parse(match.Groups[2].Value);
        var modifier = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 0;

        var rolls = Enumerable.Range(0, count).Select(_ => Random.Shared.Next(1, sides + 1)).ToArray();
        var sum = rolls.Sum() + modifier;

        var chain = new MessageChain
        {
            new TextData($"Rolling {count}d{sides}{(modifier > 0 ? "+" : "")}{(modifier != 0 ? modifier : "")}...\n"),
            new TextData($"Result: {string.Join(" + ", rolls)}{(modifier != 0 ? $" + {modifier}" : "")} = {sum}")
        };

        if (await _operation.SendRequestAsync(new SendGroupMessageRequest(e.GroupId, chain), token) is not
            { Success: true })
        {
            LogSendFailed(_logger, e.GroupId);
            return;
        }

        LogDiceSent(_logger, e.GroupId);
    }

    [GeneratedRegex(@"/dice (\d+)?d(\d+)([+-]\d+)?")]
    private static partial Regex DiceRegex();

    private readonly ILogger<DiceFunction> _logger = service.GetRequiredService<ILogger<DiceFunction>>();

    #region Log

    [LoggerMessage(EventId = 0, Level = LogLevel.Warning, Message = "Failed to send message to group {GroupId}.")]
    private static partial void LogSendFailed(ILogger logger, long groupId);

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Dice sent to group {GroupId}.")]
    private static partial void LogDiceSent(ILogger logger, long groupId);

    #endregion
}