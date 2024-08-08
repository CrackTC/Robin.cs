using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Robin.Abstractions;
using Robin.Abstractions.Context;
using Robin.Abstractions.Event.Message;
using Robin.Abstractions.Message.Entity;
using Robin.Abstractions.Operation;
using Robin.Fluent;
using Robin.Fluent.Builder;

namespace Robin.Extensions.Status;

[BotFunctionInfo("status", "当前运行状态")]
// ReSharper disable once UnusedType.Global
public partial class StatusFunction(FunctionContext context) : BotFunction(context), IFluentFunction
{
    public string? Description { get; set; }

    public Task OnCreatingAsync(FunctionBuilder builder, CancellationToken _)
    {
        builder.On<MessageEvent>()
            .OnCommand("status")
            .Do(async ctx =>
            {
                if (await ctx.Event.NewMessageRequest([
                        new TextData(
                            $"""
                            Robin Status
                            QQ号: {_context.Uin}
                            运行时间: {DateTime.Now - Process.GetCurrentProcess().StartTime}
                            总分配内存数: {GC.GetTotalAllocatedBytes() / 1024 / 1024} MB
                            当前分配内存数: {Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024} MB
                            """
                        )
                    ]).SendAsync(_context.OperationProvider, ctx.Token) is not { Success: true })
                {
                    LogSendFailed(_context.Logger, ctx.Event.SourceId);
                    return;
                }

                LogStatusSent(_context.Logger, ctx.Event.SourceId);
            });

        return Task.CompletedTask;
    }

    #region Log

    [LoggerMessage(EventId = 0, Level = LogLevel.Warning, Message = "Send message failed for {Id}")]
    private static partial void LogSendFailed(ILogger logger, long id);

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Status sent for {Id}")]
    private static partial void LogStatusSent(ILogger logger, long id);

    #endregion
}
