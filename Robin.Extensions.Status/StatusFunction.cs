using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Robin.Abstractions;
using Robin.Abstractions.Context;
using Robin.Abstractions.Event;
using Robin.Abstractions.Event.Message;
using Robin.Abstractions.Message;
using Robin.Abstractions.Message.Entity;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Requests;
using Robin.Annotations.Filters;
using Robin.Annotations.Filters.Message;

namespace Robin.Extensions.Status;

[BotFunctionInfo("status", "当前运行状态")]
[OnCommand("status")]
public partial class StatusFunction(FunctionContext context) : BotFunction(context), IFilterHandler
{
    public async Task<bool> OnFilteredEventAsync(int filterGroup, long selfId, BotEvent @event, CancellationToken token)
    {
        var status = $"Robin Status\n" +
                     $"QQ号: {_context.Uin}\n" +
                     $"运行时间: {DateTime.Now - Process.GetCurrentProcess().StartTime}\n" +
                     $"总分配内存数: {GC.GetTotalAllocatedBytes() / 1024 / 1024} MB\n" +
                     $"当前分配内存数: {Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024} MB";
        MessageChain chain = [new TextData(status)];
        Request? request = @event switch
        {
            PrivateMessageEvent e => new SendPrivateMessageRequest(e.UserId, chain),
            GroupMessageEvent e => new SendGroupMessageRequest(e.GroupId, chain),
            _ => default
        };

        var id = @event switch
        {
            PrivateMessageEvent e => e.UserId,
            GroupMessageEvent e => e.GroupId,
            _ => default
        };

        if (await request.SendAsync(_context.OperationProvider, token) is not { Success: true })
        {
            LogSendFailed(_context.Logger, id);
            return true;
        }

        LogStatusSent(_context.Logger, id);
        return true;
    }

    #region Log

    [LoggerMessage(EventId = 0, Level = LogLevel.Warning, Message = "Send message failed for {Id}")]
    private static partial void LogSendFailed(ILogger logger, long id);

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Status sent for {Id}")]
    private static partial void LogStatusSent(ILogger logger, long id);

    #endregion
}
