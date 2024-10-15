using Robin.Abstractions;
using Robin.Abstractions.Context;
using Robin.Abstractions.Event.Message;
using Robin.Abstractions.Message.Entity;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Requests;
using Robin.Fluent;
using Robin.Fluent.Builder;

namespace Robin.Extensions.Test;

[BotFunctionInfo("test", "测试功能")]
// ReSharper disable once UnusedType.Global
public class TestFunction(FunctionContext context) : BotFunction(context), IFluentFunction
{
    public string? Description { get; set; }

    public Task OnCreatingAsync(FunctionBuilder builder, CancellationToken _)
    {
        builder.On<GroupMessageEvent>()
            .Where(ctx => ctx.Event.Message.Any(segment => segment is TextData { Text: "/ping" }))
            .Select(ctx => (ctx.Event.GroupId, ctx.Token))
            .Do(ctx =>
                new SendGroupMessageRequest(ctx.GroupId, [
                    new TextData("pong!")
                ]).SendAsync(_context.OperationProvider, _context.Logger, ctx.Token)
            );

        return Task.CompletedTask;
    }
}
