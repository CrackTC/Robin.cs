using Robin.Abstractions;
using Robin.Abstractions.Context;
using Robin.Abstractions.Event.Message;
using Robin.Abstractions.Message.Entity;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Requests;
using Robin.Middlewares.Fluent;
using Robin.Middlewares.Fluent.Event;

namespace Robin.Extensions.Gray;

[BotFunctionInfo("gray", "喜多烧香精神续作（x")]
public partial class GrayFunction(FunctionContext<GrayOption> context)
    : BotFunction<GrayOption>(context),
        IFluentFunction
{
    public Task OnCreatingAsync(FunctionBuilder builder, CancellationToken token)
    {
        builder
            .On<GroupMessageEvent>()
            .OnCommand("送走")
            .OnReply()
            .DoExpensive(
                async t =>
                {
                    var (ctx, msgId) = t;

                    if (
                        await new GetMessage(msgId).SendAsync(_context, ctx.Token)
                        is not { Message.Sender.UserId: var id }
                    )
                        return false;

                    var url = $"{_context.Configuration.ApiAddress}/?id={id}";
                    await ctx
                        .Event.NewMessageRequest([new ImageData(url)])
                        .SendAsync(_context, ctx.Token);
                    return true;
                },
                t => t.EventContext,
                _context
            );

        return Task.CompletedTask;
    }
}
