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
using Robin.Abstractions.Operation.Responses;
using Robin.Annotations.Command;

namespace Robin.Extensions.Gray;

[BotFunctionInfo("gray", "send gray avatar", typeof(GroupMessageEvent))]
[OnCommand("送走")]
public partial class GrayFunction : BotFunction, ICommandHandler
{
    private readonly ILogger<GrayFunction> _logger;
    private readonly GrayOption _option;
    private static readonly HttpClient _client = new();

    public GrayFunction(
        IServiceProvider service,
        IOperationProvider operation,
        IConfiguration configuration,
        IEnumerable<BotFunction> functions) : base(service, operation, configuration, functions)
    {
        _logger = service.GetRequiredService<Logger<GrayFunction>>();
        if (configuration.Get<GrayOption>() is not GrayOption option)
        {
            LogOptionBindingFailed(_logger);
            return;
        }

        _option = option;
    }

    public override Task OnEventAsync(long selfId, BotEvent @event, CancellationToken token) => Task.CompletedTask;

    public override Task StartAsync(CancellationToken token) => Task.CompletedTask;

    public override Task StopAsync(CancellationToken token) => Task.CompletedTask;

    public async Task OnCommandAsync(long selfId, MessageEvent @event, CancellationToken token)
    {
        if (@event is not GroupMessageEvent e) return;

        var segments = e.Message.Segments.ToList();

        var text = string.Join(' ',
            segments.Where(segment => segment is TextData).Select(segment => (segment as TextData)!.Text.Trim()));

        if (segments.FirstOrDefault(segment => segment is ReplyData) is not ReplyData reply) return;

        if (await _operation.SendRequestAsync(new GetMessageRequest(reply.Id), token) is not GetMessageResponse
            {
                Success: true, Message: not null
            } originalMessage)
        {
            LogGetMessageFailed(_logger, reply.Id);
            return;
        }

        var senderId = originalMessage.Message.Sender.UserId;

        try
        {
            var image = await _client.GetByteArrayAsync($"{_option.ApiAddress}/?id={senderId}", token);
            MessageBuilder builder = [new ImageData($"base64://{Convert.ToBase64String(image)}")];
            if (await _operation.SendRequestAsync(new SendGroupMessageRequest(e.GroupId, builder.Build()), token) is not
                { Success: true })
            {
                LogSendFailed(_logger, e.GroupId);
                return;
            }
        }
        catch (Exception ex)
        {
            LogGetImageFailed(_logger, senderId.ToString(), ex);
            return;
        }

        LogImageSent(_logger, e.GroupId);
    }

    #region Log

    [LoggerMessage(EventId = 0, Level = LogLevel.Error, Message = "Failed to bind option.")]
    private static partial void LogOptionBindingFailed(ILogger logger);

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Send message failed for group {GroupId}")]
    private static partial void LogSendFailed(ILogger logger, long groupId);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Image sent for group {GroupId}")]
    private static partial void LogImageSent(ILogger logger, long groupId);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "Failed to get message {Id}")]
    private static partial void LogGetMessageFailed(ILogger logger, string id);

    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Failed to get image {Id}")]
    private static partial void LogGetImageFailed(ILogger logger, string id, Exception ex);

    #endregion
}