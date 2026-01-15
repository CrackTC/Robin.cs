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
public partial class WelcomeFunction(FunctionContext<WelcomeOption> context)
    : BotFunction<WelcomeOption>(context)
{
    public override Task StartAsync(CancellationToken token)
    {
        foreach (var (groupId, text) in _context.Configuration.WelcomeTexts)
        {
            LogWelcomeText(_context.Logger, groupId, text);
        }

        return Task.CompletedTask;
    }

    public override async Task OnEventAsync(EventContext<BotEvent> eventContext)
    {
        if (eventContext.Event is not GroupIncreaseEvent e)
            return;

        if (
            _context.Configuration.WelcomeTexts.GetValueOrDefault(e.GroupId.ToString())
            is not { } text
        )
            return;

        var parts = text.Split("{at}");

        await new SendGroupMessage(
            e.GroupId,
            [new TextData(parts[0]), new AtData(e.UserId), new TextData(parts[1])]
        ).SendAsync(_context, eventContext.Token);
    }

    #region Log

    [LoggerMessage(Level = LogLevel.Information, Message = "Welcome text for {GroupId}: {Text}")]
    private static partial void LogWelcomeText(ILogger logger, string groupId, string text);

    #endregion
}
