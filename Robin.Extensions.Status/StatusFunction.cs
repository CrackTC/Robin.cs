using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Robin.Abstractions;
using Robin.Abstractions.Communication;
using Robin.Abstractions.Event;
using Robin.Abstractions.Event.Message;
using Robin.Abstractions.Message;
using Robin.Abstractions.Message.Entities;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Requests;
using Robin.Annotations.Filters;
using Robin.Annotations.Filters.Message;

namespace Robin.Extensions.Status;

[BotFunctionInfo("status", "Display status.")]
[OnCommand("status")]
public partial class StatusFunction(
    IServiceProvider service,
    long uin,
    IOperationProvider provider,
    IConfiguration configuration,
    IEnumerable<BotFunction> functions
) : BotFunction(service, uin, provider, configuration, functions), IFilterHandler
{
    private readonly ILogger<StatusFunction> _logger = service.GetRequiredService<ILogger<StatusFunction>>();
    public async Task<bool> OnFilteredEventAsync(int filterGroup, long selfId, BotEvent @event, CancellationToken token)
    {
        var status = $"Robin Status\n" +
                     $"UIN: {_uin}\n" +
                     $"Uptime: {DateTime.Now - Process.GetCurrentProcess().StartTime}\n" +
                     $"GC Memory: {GC.GetTotalAllocatedBytes() / 1024 / 1024} MB\n" +
                     $"Total Memory: {Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024} MB";
        MessageChain chain = [new TextData(status)];
        Request? request = @event switch
        {
            PrivateMessageEvent e => new SendPrivateMessageRequest(e.UserId, chain),
            GroupMessageEvent e => new SendGroupMessageRequest(e.GroupId, chain),
            _ => default
        };

        var id = @event switch
        {
            PrivateMessageEvent e => e.UserId,
            GroupMessageEvent e => e.GroupId,
            _ => default
        };

        if (await request.SendAsync(_provider, token) is not { Success: true })
        {
            LogSendFailed(_logger, id);
            return true;
        }

        LogStatusSent(_logger, id);
        return true;
    }

    #region Log

    [LoggerMessage(EventId = 0, Level = LogLevel.Warning, Message = "Send message failed for {Id}")]
    private static partial void LogSendFailed(ILogger logger, long id);

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Status sent for {Id}")]
    private static partial void LogStatusSent(ILogger logger, long id);

    #endregion
}
