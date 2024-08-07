﻿using System.Reflection;
using System.Text;
using Microsoft.Extensions.Logging;
using Robin.Abstractions;
using Robin.Abstractions.Context;
using Robin.Abstractions.Event;
using Robin.Abstractions.Event.Message;
using Robin.Abstractions.Message;
using Robin.Abstractions.Message.Entity;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Requests;
using Robin.Annotations;
using Robin.Annotations.Filters;
using Robin.Annotations.Filters.Message;

namespace Robin.Extensions.Help;

[BotFunctionInfo("help", "帮助信息")]
[OnCommand("help")]
// ReSharper disable UnusedType.Global
public partial class HelpFunction(FunctionContext context) : BotFunction(context), IFilterHandler
{
    private static string GetTriggerDescription(BotFunctionInfoAttribute info, IEnumerable<TriggerAttribute> triggers)
    {
        var infoText = string.Join(
            " 或 ",
            info.EventTypes.Select(type => type.GetCustomAttribute<EventDescriptionAttribute>()!.Description));
        var triggerText = string.Join("\n• ", triggers
            .GroupBy(trigger => trigger is BaseEventFilterAttribute attr ? attr.FilterGroup : -1)
            .SelectMany(group => group.Key is -1 ? group.Select(trigger => Enumerable.Repeat(trigger, 1)) : [group])
            .Select(triggerGroup => string.Join(" 且 ", triggerGroup.Select(trigger => trigger.GetDescription()))));

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

        builder.Append("满足以下几组条件之一：\n• ");
        builder.Append(triggerText);

        return builder.ToString();
    }

    public async Task<bool> OnFilteredEventAsync(int filterGroup, EventContext<BotEvent> eventContext)
    {
        var descriptions = _context.Functions
            .Select(f => (
                info: f.GetType().GetCustomAttribute<BotFunctionInfoAttribute>()!,
                triggers: f.GetType().GetCustomAttributes<TriggerAttribute>())
            )
            .Select(pair =>
                $"名称: {pair.info.Name}\n描述: {pair.info.Description}{GetTriggerDescription(pair.info, pair.triggers)}");

        MessageChain chain = [new TextData(string.Join("\n\n", descriptions))];

        Request? request = eventContext.Event switch
        {
            GroupMessageEvent e => new SendGroupMessageRequest(e.GroupId, chain),
            PrivateMessageEvent e => new SendPrivateMessageRequest(e.UserId, chain),
            _ => default
        };

        var id = eventContext.Event switch
        {
            GroupMessageEvent e => e.GroupId,
            PrivateMessageEvent e => e.UserId,
            _ => default
        };

        if (await request.SendAsync(_context.OperationProvider, eventContext.Token) is not { Success: true })
        {
            LogSendFailed(_context.Logger, id);
            return true;
        }

        LogHelpSent(_context.Logger, id);
        return true;
    }

    #region Log

    [LoggerMessage(EventId = 0, Level = LogLevel.Warning, Message = "Send message failed for {Id}")]
    private static partial void LogSendFailed(ILogger logger, long id);

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Help sent for {Id}")]
    private static partial void LogHelpSent(ILogger logger, long id);

    #endregion
}
