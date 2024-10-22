using System.Text.RegularExpressions;
using Robin.Abstractions;
using Robin.Abstractions.Context;
using Robin.Abstractions.Event.Message;
using Robin.Abstractions.Message.Entity;
using Robin.Abstractions.Operation;
using Robin.Fluent;
using Robin.Fluent.Event;

namespace Robin.Extensions.Dice;

[BotFunctionInfo("dice", "投个骰子（<次数>d<面数>[+/-<修正>]）")]
// ReSharper disable once UnusedType.Global
public partial class DiceFunction(FunctionContext context) : BotFunction(context), IFluentFunction
{
    [GeneratedRegex(@"/dice (?<count>\d+)d(?<sides>\d+)(?<modifier>[+-]\d+)?")]
    private static partial Regex DiceRegex();

    public Task OnCreatingAsync(FunctionBuilder builder, CancellationToken _)
    {
        builder.On<MessageEvent>()
            .OnRegex(DiceRegex())
            .Do(async t =>
            {
                var (ctx, match) = t;

                var count = int.Min(int.Parse(match.Groups["count"].Value), 20);
                var sides = int.Parse(match.Groups["sides"].Value);
                var modifier = match.Groups["modifier"].Success
                    ? int.Parse(match.Groups["modifier"].Value)
                    : 0;

                var rolls = Enumerable.Range(0, count).Select(_ => Random.Shared.Next(sides) + 1).ToArray();
                var sum = rolls.Sum() + modifier;

                await ctx.Event.NewMessageRequest([
                    new TextData(
                        $"""
                         Rolling {count}d{sides}{(modifier > 0 ? "+" : "")}{(modifier != 0 ? modifier : "")}...
                         Result: {string.Join(" + ", rolls)}{(modifier != 0 ? $" + {modifier}" : "")} = {sum}
                         """
                    )
                ]).SendAsync(_context.OperationProvider, _context.Logger, ctx.Token);
            });

        return Task.CompletedTask;
    }
}
