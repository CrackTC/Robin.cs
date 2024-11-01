using Robin.Abstractions;
using Robin.Abstractions.Context;
using Robin.Abstractions.Event.Message;
using Robin.Abstractions.Message.Entity;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Requests;
using Robin.Middlewares.Fluent;
using Robin.Middlewares.Fluent.Event;

namespace Robin.Extensions.AtPoke;

[BotFunctionInfo("at_poke", "poke someone on at")]
public class AtPokeFunction(FunctionContext context) : BotFunction(context), IFluentFunction
{
    public Task OnCreatingAsync(FunctionBuilder builder, CancellationToken token)
    {
        builder.On<GroupMessageEvent>()
            .OnAt()
            .Do(async tuple =>
            {
                var (e, t) = tuple;
                if (e.Message.Count() > 2) return;
                if (e.Message.OfType<TextData>().FirstOrDefault() is null or { Text: not " " }) return;
                await new SendGroupPokeRequest(e.GroupId, e.Message.OfType<AtData>().Single().Uin)
                    .SendAsync(_context.BotContext.OperationProvider, _context.Logger, t);
            });

        return Task.CompletedTask;
    }
}
