using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
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
public partial class HelpFunction(FunctionContext context) : BotFunction(context), IFluentFunction
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

    private Dictionary<string, string> Helps => _context.Functions
        .Select(f => (
                function: f,
                info: f.GetType().GetCustomAttribute<BotFunctionInfoAttribute>()!,
                triggers: f.GetType().GetCustomAttributes<TriggerAttribute>()
            )
        )
        .ToDictionary(pair => pair.info.Name, pair => $"""
            名称: {pair.info.Name}
            描述: {pair.info.Description}{GetTriggerDescription(pair.function, pair.info, pair.triggers)}
            """
        );

    private Dictionary<string, string> BriefHelps => _context.Functions
        .Select(f => f.GetType().GetCustomAttribute<BotFunctionInfoAttribute>()!)
        .ToDictionary(info => info.Name, info => info.Description);

    [GeneratedRegex(@"^/help(?:\s+(?<name>\S+))?")]
    private static partial Regex HelpRegex();

    public Task OnCreatingAsync(FunctionBuilder builder, CancellationToken _)
    {
        builder.On<MessageEvent>()
            .OnAtSelf(_context.Uin)
            .OnRegex(HelpRegex())
            .Do(async tuple =>
            {
                var (ctx, match) = tuple;
                var (e, t) = ctx;

                var name = match.Groups["name"];

                if (!name.Success)
                {
                    await e.NewMessageRequest([
                        new TextData($"""
                        /help [功能名] 查看详细功能信息
                        可用功能：
                        {string.Join("\n", BriefHelps.Select(pair => $"• {pair.Key} - {pair.Value}"))}
                        """)
                    ]).SendAsync(_context.OperationProvider, _context.Logger, t);
                    return;
                }

                if (!Helps.TryGetValue(name.Value, out var help))
                {
                    await e.NewMessageRequest([
                        new TextData($"未找到功能：{name.Value}")
                    ]).SendAsync(_context.OperationProvider, _context.Logger, t);
                    return;
                }

                await e.NewMessageRequest([new TextData(help)]).SendAsync(_context.OperationProvider, _context.Logger, t);
            });

        return Task.CompletedTask;
    }
}
