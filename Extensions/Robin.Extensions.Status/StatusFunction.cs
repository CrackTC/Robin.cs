using System.Diagnostics;
using Robin.Abstractions;
using Robin.Abstractions.Context;
using Robin.Abstractions.Event.Message;
using Robin.Abstractions.Message.Entity;
using Robin.Abstractions.Operation;
using Robin.Fluent;
using Robin.Fluent.Event;

namespace Robin.Extensions.Status;

[BotFunctionInfo("status", "当前运行状态")]
// ReSharper disable once UnusedType.Global
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
                         Robin Status
                         QQ号: {_context.Uin}
                         运行时间: {DateTime.Now - Process.GetCurrentProcess().StartTime}
                         总分配内存数: {GC.GetTotalAllocatedBytes() / 1024 / 1024} MB
                         当前分配内存数: {Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024} MB
                         """
                    )
                ]).SendAsync(_context.OperationProvider, _context.Logger, ctx.Token)
            );

        return Task.CompletedTask;
    }
}
