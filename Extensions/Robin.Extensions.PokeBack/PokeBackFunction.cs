using Robin.Abstractions;
using Robin.Abstractions.Context;
using Robin.Abstractions.Event.Notice;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Requests;
using Robin.Middlewares.Fluent;
using Robin.Middlewares.Fluent.Event;

namespace Robin.Extensions.PokeBack;

[BotFunctionInfo("poke_back", "戳回去")]
// ReSharper disable once UnusedType.Global
public class PokeBackFunction(FunctionContext context) : BotFunction(context), IFluentFunction
{
    public Task OnCreatingAsync(FunctionBuilder builder, CancellationToken _)
    {
        builder.On<GroupPokeEvent>()
            .OnPokeSelf(_context.BotContext.Uin)
            .Do(ctx =>
                new SendGroupPokeRequest(ctx.Event.GroupId, ctx.Event.SenderId)
                    .SendAsync(_context.BotContext.OperationProvider, _context.Logger, ctx.Token)
            );

        return Task.CompletedTask;
    }
}
