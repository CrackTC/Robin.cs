using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Robin.Abstractions;
using Robin.Abstractions.Communication;
using Robin.Abstractions.Event;
using Robin.Abstractions.Event.Notice;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Requests;
using Robin.Annotations.Filters;
using Robin.Annotations.Filters.Notice;

namespace Robin.Extensions.PokeBack;

[BotFunctionInfo("poke_back", "Simply poke back")]
[OnPokeSelf]
// ReSharper disable once UnusedType.Global
public partial class PokeBackFunction(
    IServiceProvider service,
    long uin,
    IOperationProvider provider,
    IConfiguration configuration,
    IEnumerable<BotFunction> functions
) : BotFunction(service, uin, provider, configuration, functions), IFilterHandler
{
    private readonly ILogger<PokeBackFunction> _logger = service.GetRequiredService<ILogger<PokeBackFunction>>();

    public async Task<bool> OnFilteredEventAsync(int filterGroup, long selfId, BotEvent @event, CancellationToken token)
    {
        if (@event is not GroupPokeEvent e) return false;

        if (await new SendGroupPokeRequest(e.GroupId, e.SenderId).SendAsync(_provider, token) is not { Success: true })
        {
            LogSendFailed(_logger, e.GroupId);
            return true;
        }

        LogPokeSent(_logger, e.GroupId);
        return true;
    }

    #region Log

    [LoggerMessage(EventId = 0, Level = LogLevel.Error, Message = "Send poke to group {GroupId} failed")]
    private static partial void LogSendFailed(ILogger logger, long groupId);

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Poke sent to group {GroupId}")]
    private static partial void LogPokeSent(ILogger logger, long groupId);

    #endregion
}