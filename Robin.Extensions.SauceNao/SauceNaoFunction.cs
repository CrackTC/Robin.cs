using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Robin.Abstractions;
using Robin.Abstractions.Communication;
using Robin.Abstractions.Event;
using Robin.Abstractions.Event.Message;
using Robin.Abstractions.Message.Entities;
using Robin.Abstractions.Operation.Requests;
using Robin.Abstractions.Operation.Responses;
using Robin.Annotations.Filters;
using Robin.Annotations.Filters.Message;
using SauceNET;

namespace Robin.Extensions.SauceNao;

// ReSharper disable once UnusedType.Global
[BotFunctionInfo("sauce_nao", "Search for the source of an image.")]
[OnReply, OnCommand("搜图")]
public partial class SauceNaoFunction(
    IServiceProvider service,
    long uin,
    IOperationProvider operation,
    IConfiguration configuration,
    IEnumerable<BotFunction> functions)
    : BotFunction(service, uin, operation, configuration, functions), IFilterHandler
{
    public override Task OnEventAsync(long selfId, BotEvent @event, CancellationToken token) => throw new InvalidOperationException();

    public override Task StartAsync(CancellationToken token)
    {
        if (_configuration.Get<SauceNaoOption>() is not { } option)
        {
            LogOptionBindingFailed(_logger);
            return Task.CompletedTask;
        }

        _client = new SauceNETClient(option.ApiKey);

        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken token) => Task.CompletedTask;

    public async Task<bool> OnFilteredEventAsync(int filterGroup, long selfId, BotEvent @event, CancellationToken token)
    {
        if (@event is not GroupMessageEvent e) return false;

        var reply = e.Message.OfType<ReplyData>().First();

        if (await _operation.SendRequestAsync(new GetMessageRequest(reply.Id), token) is not GetMessageResponse
            {
                Success: true, Message: not null
            } originalMessage)
        {
            LogGetMessageFailed(_logger, reply.Id);
            return true;
        }

        if (originalMessage.Message.Message.FirstOrDefault(segment => segment is ImageData) is not ImageData image)
            return true;

        var sauce = await _client!.GetSauceAsync(image.Url);
        var results = sauce.Results
            .Where(result => double.TryParse(result.Similarity, out var s) && s >= 70.0)
            .Take(3)
            .Select(result =>
                string.Join(
                    '\n',
                    $"![{result.Name}]({result.ThumbnailURL})\n",
                    $"**相似度**: {result.Similarity}%\n",
                    $"**来源**: {result.DatabaseName}\n",
                    $"**标题**: {result.Name}\n",
                    $"**链接**: {result.SourceURL}\n"
                )
            ).ToList();

        if (results.Count == 0)
        {
            if (await _operation.SendRequestAsync(
                    new SendGroupMessageRequest(e.GroupId, [new TextData("找不到喵>_<")]), token) is not
                    { Success: true })
            {
                LogSendMessageFailed(_logger, e.GroupId);
                return true;
            }

            LogMessageSent(_logger, e.GroupId);
            return true;
        }

        var textData = new TextData(string.Join("---\n", results));

        if (await _operation.SendRequestAsync(new SendGroupMessageRequest(e.GroupId, [textData]), token) is not { Success: true })
        {
            LogSendMessageFailed(_logger, e.GroupId);
            return true;
        }

        LogMessageSent(_logger, e.GroupId);
        return true;
    }

    private readonly ILogger<SauceNaoFunction> _logger = service.GetRequiredService<ILogger<SauceNaoFunction>>();
    private SauceNETClient? _client;

    #region Log

    [LoggerMessage(EventId = 0, Level = LogLevel.Warning, Message = "Failed to get message {Id}")]
    private static partial void LogGetMessageFailed(ILogger logger, string id);

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Option binding failed")]
    private static partial void LogOptionBindingFailed(ILogger logger);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Failed to send message to {GroupId}")]
    private static partial void LogSendMessageFailed(ILogger logger, long groupId);

    [LoggerMessage(EventId = 4, Level = LogLevel.Information, Message = "Message sent to {GroupId}")]
    private static partial void LogMessageSent(ILogger logger, long groupId);

    #endregion
}