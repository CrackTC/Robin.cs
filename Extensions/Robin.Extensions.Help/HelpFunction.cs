using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Robin.Abstractions;
using Robin.Abstractions.Context;
using Robin.Abstractions.Event;
using Robin.Abstractions.Event.Message;
using Robin.Abstractions.Message.Entity;
using Robin.Abstractions.Operation;
using Robin.Middlewares.Fluent;
using Robin.Middlewares.Fluent.Event;

namespace Robin.Extensions.Help;

[BotFunctionInfo("help", "帮助信息")]
public partial class HelpFunction(FunctionContext context) : BotFunction(context), IFluentFunction
{
    private static string GetTriggerDescription(
        BotFunction function,
        BotFunctionInfoAttribute info
    )
    {
        var infoText = string.Join(" 或 ", info.EventTypes
            .Select(type => type.GetCustomAttribute<EventDescriptionAttribute>()!.Description));

        var tunnelText = string.Concat(function.TriggerDescriptions
            .Select(tunnelDesc => $"• {tunnelDesc}\n"));

        if (string.IsNullOrEmpty(infoText) && string.IsNullOrEmpty(tunnelText)) return string.Empty;

        var builder = new StringBuilder("\n触发条件：\n");
        if (!string.IsNullOrEmpty(infoText))
        {
            builder.Append("收到 ").Append(infoText);
            if (!string.IsNullOrEmpty(tunnelText)) builder.Append("，或");
        }

        if (string.IsNullOrEmpty(tunnelText)) return builder.ToString();

        builder.AppendLine("满足以下几组条件之一：").Append(tunnelText);

        return builder.ToString();
    }

    [GeneratedRegex(@"^/help(?:\s+(?<name>\S+))?")]
    private static partial Regex HelpRegex { get; }

    public Task OnCreatingAsync(FunctionBuilder builder, CancellationToken _)
    {
        var helps = _context.BotContext.Functions
            .Select(f => (Func: f, Info: f.GetType().GetCustomAttribute<BotFunctionInfoAttribute>()!))
            .ToDictionary(t => t.Info.Name, t => (
                Func: t.Func,
                Help: new Func<string>(() =>
                    $"""
                    名称: {t.Info.Name}
                    描述: {t.Info.Description}{GetTriggerDescription(t.Func, t.Info)}
                    """
            )));

        var briefHelps = _context.BotContext.Functions
            .Select(f => (Func: f, Info: f.GetType().GetCustomAttribute<BotFunctionInfoAttribute>()!))
            .Select(t => (t.Info.Name, t.Func, t.Info.Description))
            .ToList();

        builder.On<MessageEvent>()
            .OnAt(_context.BotContext.Uin)
            .OnRegex(HelpRegex)
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
                            {
                                string.Join('\n', briefHelps.Where(help => e switch
                                {
                                    IGroupEvent {GroupId: var id} => help.Func.Context.GroupFilter.IsIdEnabled(id),
                                    IPrivateEvent {UserId: var id} => help.Func.Context.PrivateFilter.IsIdEnabled(id),
                                    _ => true
                                }).Select(help => $"• {help.Name} - {help.Description}"))
                            }
                            """)
                    ]).SendAsync(_context, t);
                    return;
                }

                if (!helps.TryGetValue(name.Value, out var help))
                {
                    await e.NewMessageRequest([new TextData($"未找到功能：{name.Value}")]).SendAsync(_context, t);
                    return;
                }

                if (e switch
                {
                    IGroupEvent { GroupId: var id } => !help.Func.Context.GroupFilter.IsIdEnabled(id),
                    IPrivateEvent { UserId: var id } => !help.Func.Context.PrivateFilter.IsIdEnabled(id),
                    _ => false
                })
                {
                    await e.NewMessageRequest([new TextData($"功能 {name.Value} 在当前上下文中被禁用")]).SendAsync(_context, t);
                    return;
                }

                await e.NewMessageRequest([new TextData(help.Help())]).SendAsync(_context, t);
            });

        return Task.CompletedTask;
    }
}
