using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Robin.Abstractions;
using Robin.Abstractions.Context;
using Robin.Abstractions.Event;
using Robin.Abstractions.Event.Message;
using Robin.Abstractions.Message.Entity;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Requests;
using Robin.Annotations.Cron;
using Robin.Annotations.Filters;
using Robin.Annotations.Filters.Message;

namespace Robin.Extensions.WordCloud;

[BotFunctionInfo("word_cloud", "群词云生成", typeof(GroupMessageEvent))]
[OnCommand("word_cloud")]
[OnCron("0 0 0 * * ?")]
// ReSharper disable once UnusedType.Global
public partial class WordCloudFunction(FunctionContext context) : BotFunction(context), IFilterHandler, ICronHandler
{
    private WordCloudOption? _option;
    private static readonly HttpClient _client = new();

    private readonly WordCloudDbContext _db = new(context.Uin);
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private Task<bool> CreateTableAsync(CancellationToken token)
        => _db.Database.EnsureCreatedAsync(token);

    private async Task InsertDataAsync(long groupId, string message, CancellationToken token)
    {
        await _semaphore.WaitAsync(token);
        try
        {
            await _db.Records.AddAsync(new Record
            {
                GroupId = groupId,
                Content = message
            }, token);
            await _db.SaveChangesAsync(token);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<IEnumerable<long>> GetGroupsAsync(CancellationToken token)
    {
        await _semaphore.WaitAsync(token);

        try
        {
            return await _db.Records.Select(r => r.GroupId).Distinct().ToListAsync(token);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<IEnumerable<string>> GetGroupMessagesAsync(long groupId, CancellationToken token)
    {
        await _semaphore.WaitAsync(token);
        try
        {
            return await _db.Records.Where(r => r.GroupId == groupId).Select(r => r.Content).ToListAsync(token);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task ClearGroupMessagesAsync(long groupId, CancellationToken token)
    {
        await _semaphore.WaitAsync(token);
        try
        {
            _db.Records.RemoveRange(_db.Records.Where(r => r.GroupId == groupId));
            await _db.SaveChangesAsync(token);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task SendWordCloudAsync(long groupId, bool clear = false, CancellationToken token = default)
    {
        var messages = await GetGroupMessagesAsync(groupId, token);
        var content = string.Join('\n', messages);
        using var response = await _client.PostAsJsonAsync(_option!.ApiAddress,
            _option.CloudOption with
            {
                Text = content
            }, cancellationToken: token);

        if (!response.IsSuccessStatusCode)
        {
            LogApiRequestFailed(_context.Logger, groupId);
            return;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(token);
        var url = (await JsonNode.ParseAsync(stream, cancellationToken: token))?["url"]?.ToString();

        if (url is null)
        {
            LogApiRequestFailed(_context.Logger, groupId);
            return;
        }

        if (clear) await ClearGroupMessagesAsync(groupId, token);

        if (await new SendGroupMessageRequest(groupId, [
                new ImageData(url)
            ]).SendAsync(_context.OperationProvider, token) is not { Success: true })
        {
            LogSendFailed(_context.Logger, groupId);
            return;
        }

        LogWordCloudSent(_context.Logger, groupId);
    }

    public override async Task OnEventAsync(long selfId, BotEvent @event, CancellationToken token)
    {
        if (@event is not GroupMessageEvent e) return;

        await InsertDataAsync(e.GroupId, string.Join(' ', e.Message.OfType<TextData>().Select(s => s.Text)), token);
    }

    public override async Task StartAsync(CancellationToken token)
    {
        if (_context.Configuration.Get<WordCloudOption>() is not { } option)
        {
            LogOptionBindingFailed(_context.Logger);
            return;
        }

        _option = option;

        await CreateTableAsync(token);
    }

    public override async Task StopAsync(CancellationToken token)
    {
        _client.Dispose();
        _semaphore.Dispose();
        await _db.DisposeAsync();
    }

    public async Task<bool> OnFilteredEventAsync(int filterGroup, long selfId, BotEvent @event, CancellationToken token)
    {
        if (@event is not GroupMessageEvent e) return false;
        await SendWordCloudAsync(e.GroupId, token: token);
        return true;
    }

    public async Task OnCronEventAsync(CancellationToken token)
    {
        try
        {
            await Task.WhenAll((await GetGroupsAsync(token)).Select(group => SendWordCloudAsync(group, true, token)));
        }
        catch (Exception e)
        {
            LogExceptionOccurred(_context.Logger, e);
        }
    }

    #region Log

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Option binding failed")]
    private static partial void LogOptionBindingFailed(ILogger logger);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning,
        Message = "Word cloud api request failed for group {GroupId}")]
    private static partial void LogApiRequestFailed(ILogger logger, long groupId);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "Send message failed for group {GroupId}")]
    private static partial void LogSendFailed(ILogger logger, long groupId);

    [LoggerMessage(EventId = 4, Level = LogLevel.Information, Message = "Word cloud sent for group {GroupId}")]
    private static partial void LogWordCloudSent(ILogger logger, long groupId);

    [LoggerMessage(EventId = 5, Level = LogLevel.Warning, Message = "Exception occurred while sending word cloud")]
    private static partial void LogExceptionOccurred(ILogger logger, Exception exception);

    #endregion
}