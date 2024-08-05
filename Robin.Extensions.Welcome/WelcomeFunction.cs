using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Robin.Abstractions;
using Robin.Abstractions.Communication;
using Robin.Abstractions.Event;
using Robin.Abstractions.Event.Notice.Member.Increase;
using Robin.Abstractions.Message.Entities;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Requests;
using Robin.Annotations.Filters;
using Robin.Annotations.Filters.Notice;

namespace Robin.Extensions.Welcome;

[BotFunctionInfo("welcome", "入群欢迎")]
[OnMemberIncrease]
public partial class WelcomeFunction(
    IServiceProvider service,
    long uin,
    IOperationProvider provider,
    IConfiguration configuration,
    IEnumerable<BotFunction> functions
) : BotFunction(service, uin, provider, configuration, functions), IFilterHandler
{
    private readonly ILogger<WelcomeFunction> _logger = service.GetRequiredService<ILogger<WelcomeFunction>>();

    private WelcomeOption? _option;

    public override Task StartAsync(CancellationToken token)
    {
        if (_configuration.Get<WelcomeOption>() is not { } option)
        {
            LogOptionBindingFailed(_logger);
            return Task.CompletedTask;
        }

        _option = option;

        foreach (var (groupId, text) in option.WelcomeTexts)
        {
            LogWelcomeText(_logger, groupId, text);
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
                new TextData(parts[1])
            ]).SendAsync(_provider, token) is not { Success: true })
        {
            LogSendMessageFailed(_logger, e.GroupId);
            return true;
        }

        LogMessageSent(_logger, e.GroupId);
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
