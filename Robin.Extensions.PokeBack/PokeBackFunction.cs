using Microsoft.Extensions.Logging;
using Robin.Abstractions;
using Robin.Abstractions.Context;
using Robin.Abstractions.Event;
using Robin.Abstractions.Event.Notice;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Requests;
using Robin.Annotations.Filters;
using Robin.Annotations.Filters.Notice;

namespace Robin.Extensions.PokeBack;

[BotFunctionInfo("poke_back", "戳回去")]
[OnPokeSelf]
// ReSharper disable once UnusedType.Global
public partial class PokeBackFunction(FunctionContext context) : BotFunction(context), IFilterHandler
{
    public async Task<bool> OnFilteredEventAsync(int filterGroup, long selfId, BotEvent @event, CancellationToken token)
    {
        if (@event is not GroupPokeEvent e) return false;

        if (await new SendGroupPokeRequest(e.GroupId, e.SenderId)
            .SendAsync(_context.OperationProvider, token) is not { Success: true })
        {
            LogSendFailed(_context.Logger, e.GroupId);
            return true;
        }

        LogPokeSent(_context.Logger, e.GroupId);
        return true;
    }

    #region Log

    [LoggerMessage(EventId = 0, Level = LogLevel.Error, Message = "Send poke to group {GroupId} failed")]
    private static partial void LogSendFailed(ILogger logger, long groupId);

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Poke sent to group {GroupId}")]
    private static partial void LogPokeSent(ILogger logger, long groupId);

    #endregion
}