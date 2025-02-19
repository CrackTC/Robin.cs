using Robin.Abstractions;
using Robin.Abstractions.Context;
using Robin.Abstractions.Event.Message;
using Robin.Abstractions.Message.Entity;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Requests;
using Robin.Middlewares.Fluent;

namespace Robin.Extensions.Test;

[BotFunctionInfo("test", "测试功能")]
public class TestFunction(FunctionContext context) : BotFunction(context), IFluentFunction
{
    public Task OnCreatingAsync(FunctionBuilder builder, CancellationToken _)
    {
        builder.On<GroupMessageEvent>()
            .Where(ctx => ctx.Event.Message.Any(segment => segment is TextData { Text: "/ping" }))
            .Select(ctx => (ctx.Event.GroupId, ctx.Token))
            .Do(ctx =>
                new SendGroupMessageRequest(ctx.GroupId, [new TextData("pong!")]).SendAsync(_context, ctx.Token)
            );

        return Task.CompletedTask;
    }
}
