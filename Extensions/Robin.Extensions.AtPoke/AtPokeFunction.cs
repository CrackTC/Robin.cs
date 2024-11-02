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

                long uin = e.Message switch { [AtData at, TextData { Text: " " }] => at.Uin, [AtData at] => at.Uin, _ => 0 };
                if (uin is 0) return;

                await new SendGroupPokeRequest(e.GroupId, uin)
                    .SendAsync(_context.BotContext.OperationProvider, _context.Logger, t);
            });

        return Task.CompletedTask;
    }
}
