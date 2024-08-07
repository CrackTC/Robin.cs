using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Robin.Abstractions;
using Robin.Abstractions.Context;
using Robin.Abstractions.Event;
using Robin.Abstractions.Event.Message;
using Robin.Abstractions.Message;
using Robin.Abstractions.Message.Entity;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Requests;
using Robin.Annotations.Filters;
using Robin.Annotations.Filters.Message;

namespace Robin.Extensions.Dice;

[BotFunctionInfo("dice", "投个骰子（<次数>d<面数>[+/-<修正>]）")]
[OnCommand("dice")]
// ReSharper disable once UnusedType.Global
public partial class DiceFunction(FunctionContext context) : BotFunction(context), IFilterHandler
{
    public async Task<bool> OnFilteredEventAsync(int filterGroup, EventContext eventContext)
    {
        if (eventContext.Event is not GroupMessageEvent e) return false;

        var text = string.Join("", e.Message
            .OfType<TextData>()
            .Select(data => data.Text.Trim())).Trim();

        if (string.IsNullOrEmpty(text)) return false;

        var match = DiceRegex().Match(text);
        if (!match.Success) return false;

        var count = int.Min(match.Groups[1].Success ? int.Parse(match.Groups[1].Value) : 1, 20);
        var sides = int.Parse(match.Groups[2].Value);
        var modifier = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 0;

        var rolls = Enumerable.Range(0, count).Select(_ => Random.Shared.Next(1, sides + 1)).ToArray();
        var sum = rolls.Sum() + modifier;

        var chain = new MessageChain
        {
            new TextData($"Rolling {count}d{sides}{(modifier > 0 ? "+" : "")}{(modifier != 0 ? modifier : "")}...\n"),
            new TextData($"Result: {string.Join(" + ", rolls)}{(modifier != 0 ? $" + {modifier}" : "")} = {sum}")
        };

        if (await new SendGroupMessageRequest(e.GroupId, chain).SendAsync(_context.OperationProvider, eventContext.Token) is not { Success: true })
        {
            LogSendFailed(_context.Logger, e.GroupId);
            return true;
        }

        LogDiceSent(_context.Logger, e.GroupId);
        return true;
    }

    [GeneratedRegex(@"/dice (\d+)?d(\d+)([+-]\d+)?")]
    private static partial Regex DiceRegex();

    #region Log

    [LoggerMessage(EventId = 0, Level = LogLevel.Warning, Message = "Failed to send message to group {GroupId}.")]
    private static partial void LogSendFailed(ILogger logger, long groupId);

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Dice sent to group {GroupId}.")]
    private static partial void LogDiceSent(ILogger logger, long groupId);

    #endregion
}
