using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Robin.Abstractions;
using Robin.Abstractions.Context;
using Robin.Abstractions.Event;
using Robin.Abstractions.Event.Notice.Member.Increase;
using Robin.Abstractions.Message.Entity;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Requests;
using Robin.Annotations.Filters;
using Robin.Annotations.Filters.Notice;

namespace Robin.Extensions.Welcome;

[BotFunctionInfo("welcome", "入群欢迎")]
[OnMemberIncrease]
public partial class WelcomeFunction(FunctionContext context) : BotFunction(context), IFilterHandler
{
    private WelcomeOption? _option;

    public override Task StartAsync(CancellationToken token)
    {
        if (_context.Configuration.Get<WelcomeOption>() is not { } option)
        {
            LogOptionBindingFailed(_context.Logger);
            return Task.CompletedTask;
        }

        _option = option;

        foreach (var (groupId, text) in option.WelcomeTexts)
        {
            LogWelcomeText(_context.Logger, groupId, text);
        }

        return Task.CompletedTask;
    }

    public async Task<bool> OnFilteredEventAsync(int filterGroup, long selfId, BotEvent @event, CancellationToken token)
    {
        if (@event is not GroupIncreaseEvent e) return false;

        if (_option?.WelcomeTexts.TryGetValue(e.GroupId.ToString(), out var text) is not true) return true;

        var parts = text.Split("{at}");

        if (await new SendGroupMessageRequest(e.GroupId, [
            new TextData(parts[0]),
            new AtData(e.UserId),
            new TextData(parts[1]),
        ]).SendAsync(_context.OperationProvider, token) is not { Success: true })
        {
            LogSendMessageFailed(_context.Logger, e.GroupId);
            return true;
        }

        LogMessageSent(_context.Logger, e.GroupId);
        return true;
    }

    #region Log

    [LoggerMessage(EventId = 0, Level = LogLevel.Warning, Message = "Option binding failed")]
    private static partial void LogOptionBindingFailed(ILogger logger);

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Message sent to {GroupId}")]
    private static partial void LogMessageSent(ILogger logger, long groupId);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Failed to send message to {GroupId}")]
    private static partial void LogSendMessageFailed(ILogger logger, long groupId);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "Welcome text for {GroupId}: {Text}")]
    private static partial void LogWelcomeText(ILogger logger, string groupId, string text);

    #endregion
}
