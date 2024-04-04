using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Robin.Abstractions;
using Robin.Abstractions.Communication;
using Robin.Abstractions.Event;
using Robin.Abstractions.Event.Message;
using Robin.Abstractions.Message;
using Robin.Abstractions.Message.Entities;
using Robin.Abstractions.Operation.Requests;
using Robin.Annotations.Command;

namespace Robin.Extensions.RandReply;

[BotFunctionInfo("rand_reply", "Random Reply")]
[OnCommand("", at: true)]
public partial class RandReplyFunction(
    IServiceProvider service,
    long uin,
    IOperationProvider operation,
    IConfiguration configuration,
    IEnumerable<BotFunction> functions)
    : BotFunction(service, uin, operation, configuration, functions), ICommandHandler
{
    private RandReplyOption? _option;
    private readonly ILogger<RandReplyFunction> _logger = service.GetRequiredService<ILogger<RandReplyFunction>>();

    public override Task OnEventAsync(long selfId, BotEvent @event, CancellationToken token) => Task.CompletedTask;

    public async Task OnCommandAsync(long selfId, MessageEvent @event, CancellationToken token)
    {
        if (@event is not GroupMessageEvent e) return;

        var textCount = _option!.Texts?.Count ?? 0;
        var imageCount = _option.ImagePaths?.Count ?? 0;
        var index = Random.Shared.Next(textCount + imageCount);

        MessageChain chain =
        [
            index < textCount
                ? new TextData(_option.Texts![index])
                : new ImageData(
                    $"base64://{Convert.ToBase64String(await File.ReadAllBytesAsync(_option.ImagePaths![index - textCount], token))}")
        ];

        if (await _operation.SendRequestAsync(new SendGroupMessageRequest(e.GroupId, chain), token) is not
            { Success: true })
        {
            LogSendFailed(_logger, e.GroupId);
            return;
        }

        LogReplySent(_logger, e.GroupId);
    }

    public override Task StartAsync(CancellationToken token)
    {
        if (_configuration.Get<RandReplyOption>() is not { } option)
        {
            LogOptionBindingFailed(_logger);
            return Task.CompletedTask;
        }

        _option = option;
        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken token) => Task.CompletedTask;

    #region Log

    [LoggerMessage(EventId = 0, Level = LogLevel.Error, Message = "Failed to bind option.")]
    private static partial void LogOptionBindingFailed(ILogger logger);

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Send message failed for group {GroupId}")]
    private static partial void LogSendFailed(ILogger logger, long groupId);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Reply sent for group {GroupId}")]
    private static partial void LogReplySent(ILogger logger, long groupId);

    #endregion
}
