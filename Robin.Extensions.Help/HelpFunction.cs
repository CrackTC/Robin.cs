using System.Reflection;
using System.Text;
using Robin.Abstractions;
using Robin.Abstractions.Context;
using Robin.Abstractions.Event;
using Robin.Abstractions.Event.Message;
using Robin.Abstractions.Message.Entity;
using Robin.Abstractions.Operation;
using Robin.Annotations;
using Robin.Fluent;
using Robin.Fluent.Builder;

namespace Robin.Extensions.Help;

[BotFunctionInfo("help", "帮助信息")]
// ReSharper disable UnusedType.Global
public class HelpFunction(FunctionContext context) : BotFunction(context), IFluentFunction
{
    public string? Description { get; set; }

    private static string GetTriggerDescription(
        BotFunction function,
        BotFunctionInfoAttribute info,
        IEnumerable<TriggerAttribute> triggers
    )
    {
        var infoText = string.Join(" 或 ", info.EventTypes
            .Select(type => type.GetCustomAttribute<EventDescriptionAttribute>()!.Description));

        var triggerText = string.Join(null, triggers
            .Select(trigger => $"• {trigger.GetDescription()}\n"));

        triggerText += (function as IFluentFunction)?.Description;

        if (string.IsNullOrEmpty(infoText) && string.IsNullOrEmpty(triggerText)) return string.Empty;

        var builder = new StringBuilder("\n触发条件：\n");
        if (!string.IsNullOrEmpty(infoText))
        {
            builder.Append("收到 ");
            builder.Append(infoText);

            if (!string.IsNullOrEmpty(triggerText))
            {
                builder.Append("，或");
            }
        }

        if (string.IsNullOrEmpty(triggerText)) return builder.ToString();

        builder.AppendLine("满足以下几组条件之一：");
        builder.Append(triggerText);

        return builder.ToString();
    }

    public Task OnCreatingAsync(FunctionBuilder builder, CancellationToken _)
    {
        builder.On<MessageEvent>()
            .OnCommand("help")
            .Do(async ctx =>
            {
                await ctx.Event.NewMessageRequest([
                    new TextData(string.Join("\n\n", _context.Functions
                        .Select(f => (
                                function: f,
                                info: f.GetType().GetCustomAttribute<BotFunctionInfoAttribute>()!,
                                triggers: f.GetType().GetCustomAttributes<TriggerAttribute>()
                            )
                        )
                        .Select(pair =>
                            $"""
                             名称: {pair.info.Name}
                             描述: {pair.info.Description}{GetTriggerDescription(pair.function, pair.info, pair.triggers)}
                             """
                        )
                    ))
                ]).SendAsync(_context.OperationProvider, _context.Logger, ctx.Token);
            });

        return Task.CompletedTask;
    }
}
