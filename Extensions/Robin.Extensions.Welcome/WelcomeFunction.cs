using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Robin.Abstractions;
using Robin.Abstractions.Context;
using Robin.Abstractions.Event;
using Robin.Abstractions.Event.Notice.Member.Increase;
using Robin.Abstractions.Message.Entity;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Requests;

namespace Robin.Extensions.Welcome;

[BotFunctionInfo("welcome", "入群欢迎", typeof(GroupIncreaseEvent))]
// ReSharper disable once UnusedType.Global
public partial class WelcomeFunction(FunctionContext context) : BotFunction(context)
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

    public override async Task OnEventAsync(EventContext<BotEvent> eventContext)
    {
        if (eventContext.Event is not GroupIncreaseEvent e) return;

        if (_option?.WelcomeTexts.TryGetValue(e.GroupId.ToString(), out var text) is not true) return;

        var parts = text.Split("{at}");

        await new SendGroupMessageRequest(e.GroupId, [
            new TextData(parts[0]),
            new AtData(e.UserId),
            new TextData(parts[1])
        ]).SendAsync(_context.OperationProvider, _context.Logger, eventContext.Token);
    }

    #region Log

    [LoggerMessage(EventId = 0, Level = LogLevel.Warning, Message = "Option binding failed")]
    private static partial void LogOptionBindingFailed(ILogger logger);

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Welcome text for {GroupId}: {Text}")]
    private static partial void LogWelcomeText(ILogger logger, string groupId, string text);

    #endregion
}
