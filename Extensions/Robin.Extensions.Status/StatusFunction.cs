using System.Diagnostics;
using Robin.Abstractions;
using Robin.Abstractions.Context;
using Robin.Abstractions.Event.Message;
using Robin.Abstractions.Message.Entity;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Requests;
using Robin.Middlewares.Fluent;
using Robin.Middlewares.Fluent.Event;

namespace Robin.Extensions.Status;

[BotFunctionInfo("status", "当前运行状态")]
public class StatusFunction(FunctionContext context) : BotFunction(context), IFluentFunction
{
    public Task OnCreatingAsync(FunctionBuilder builder, CancellationToken _)
    {
        static double GetMemoryUsageMB() => Process.GetCurrentProcess().WorkingSet64 / 1048576.0;
        static double GetTotalMemoryMB() =>
            GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / 1048576.0;

        builder
            .On<MessageEvent>()
            .OnCommand("status")
            .Do(async ctx =>
            {
                if (
                    await new GetFriendList().SendAsync(_context, ctx.Token)
                    is not { Friends.Count: var friendCount }
                )
                    return;
                if (
                    await new GetGroupList().SendAsync(_context, ctx.Token)
                    is not { Groups.Count: var groupCount }
                )
                    return;
                await ctx
                    .Event.NewMessageRequest([
                        new TextData(
                            $"""
                            Robin（https://github.com/CrackTC/Robin.cs）
                            QQ号: {_context.BotContext.Uin}
                            好友数: {friendCount}
                            群组数: {groupCount}
                            运行时间: {DateTime.Now - Process.GetCurrentProcess().StartTime}
                            GC合计分配内存: {GC.GetTotalAllocatedBytes() / 1048576.0:F2} MB
                            当前工作集大小: {GetMemoryUsageMB():F2} / {GetTotalMemoryMB():F2} MB
                            """
                        ),
                    ])
                    .SendAsync(_context, ctx.Token);
            });

        return Task.CompletedTask;
    }
}
