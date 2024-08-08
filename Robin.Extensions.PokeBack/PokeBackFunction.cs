using Microsoft.Extensions.Logging;
using Robin.Abstractions;
using Robin.Abstractions.Context;
using Robin.Abstractions.Event.Notice;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Requests;
using Robin.Fluent;
using Robin.Fluent.Builder;

namespace Robin.Extensions.PokeBack;

[BotFunctionInfo("poke_back", "戳回去")]
// ReSharper disable once UnusedType.Global
public partial class PokeBackFunction(FunctionContext context) : BotFunction(context), IFluentFunction
{
    public string? Description { get; set; }

    public Task OnCreatingAsync(FunctionBuilder builder, CancellationToken _)
    {
        builder.On<GroupPokeEvent>()
            .OnPokeSelf(_context.Uin)
            .Do(async ctx =>
            {
                var e = ctx.Event;
                if (await new SendGroupPokeRequest(e.GroupId, e.SenderId)
                    .SendAsync(_context.OperationProvider, ctx.Token) is not { Success: true })
                {
                    LogSendFailed(_context.Logger, e.GroupId);
                    return;
                }

                LogPokeSent(_context.Logger, e.GroupId);
            });

        return Task.CompletedTask;
    }

    #region Log

    [LoggerMessage(EventId = 0, Level = LogLevel.Error, Message = "Send poke to group {GroupId} failed")]
    private static partial void LogSendFailed(ILogger logger, long groupId);

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Poke sent to group {GroupId}")]
    private static partial void LogPokeSent(ILogger logger, long groupId);

    #endregion
}