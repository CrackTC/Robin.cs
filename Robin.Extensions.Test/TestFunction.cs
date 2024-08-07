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
public class TestFunction(FunctionContext context) : BotFunction(context), IFluentFunction
{
    public IEnumerable<string> Descriptions { get; set; } = [];

    public void OnCreating(FunctionBuilder functionBuilder)
    {
        functionBuilder.On<GroupMessageEvent>()
            .Where(ctx => ctx.Event.Message.Any(segment => segment is TextData { Text: "/test" }))
            .Select(ctx => (ctx.Event.GroupId, ctx.Token))
            .Do(async ctx =>
            {
                await new SendGroupMessageRequest(ctx.GroupId, [
                    new TextData("Hello, world")
                ]).SendAsync(_context.OperationProvider, ctx.Token);
            });
    }
}
