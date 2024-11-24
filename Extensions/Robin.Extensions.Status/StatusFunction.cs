using System.Diagnostics;
using Robin.Abstractions;
using Robin.Abstractions.Context;
using Robin.Abstractions.Event.Message;
using Robin.Abstractions.Message.Entity;
using Robin.Abstractions.Operation;
using Robin.Middlewares.Fluent;
using Robin.Middlewares.Fluent.Event;

namespace Robin.Extensions.Status;

[BotFunctionInfo("status", "当前运行状态")]
public class StatusFunction(FunctionContext context) : BotFunction(context), IFluentFunction
{
    public Task OnCreatingAsync(FunctionBuilder builder, CancellationToken _)
    {
        builder.On<MessageEvent>()
            .OnCommand("status")
            .Do(ctx =>
                ctx.Event.NewMessageRequest([
                    new TextData(
                        $"""
                         Robin（https://github.com/CrackTC/Robin.cs）
                         QQ号: {_context.BotContext.Uin}
                         运行时间: {DateTime.Now - Process.GetCurrentProcess().StartTime}
                         GC合计分配内存: {GC.GetTotalAllocatedBytes() / 1024 / 1024} MB
                         当前工作集大小: {Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024} MB
                         """
                    )
                ]).SendAsync(_context, ctx.Token)
            );

        return Task.CompletedTask;
    }
}
