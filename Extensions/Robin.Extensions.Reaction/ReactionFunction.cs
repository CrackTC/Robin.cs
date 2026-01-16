using Robin.Abstractions;
using Robin.Abstractions.Context;
using Robin.Abstractions.Event.Notice.Reaction;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Requests;
using Robin.Middlewares.Fluent;

namespace Robin.Extensions.Reaction;

[BotFunctionInfo("reaction", "跟风贴表情")]
public class ReactionFunction(FunctionContext context) : BotFunction(context), IFluentFunction
{
    public Task OnCreatingAsync(FunctionBuilder builder, CancellationToken token)
    {
        builder
            .On<ReactionAddEvent>()
            .Where(ctx => ctx.Event.Count is 2)
            .Do(async ctx =>
            {
                await new SetGroupReaction(
                    ctx.Event.GroupId,
                    ctx.Event.MessageId,
                    ctx.Event.Code,
                    true
                ).SendAsync(_context, ctx.Token);
            });

        return Task.CompletedTask;
    }
}
