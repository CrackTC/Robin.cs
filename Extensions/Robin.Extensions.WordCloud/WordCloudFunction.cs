using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Robin.Abstractions;
using Robin.Abstractions.Context;
using Robin.Abstractions.Event.Message;
using Robin.Abstractions.Message.Entity;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Requests;
using Robin.Middlewares.Annotations.Cron;
using Robin.Middlewares.Fluent;
using Robin.Middlewares.Fluent.Event;

namespace Robin.Extensions.WordCloud;

[BotFunctionInfo("word_cloud", "群词云生成")]
[OnCron("0 0 0 * * ?")]
// ReSharper disable once UnusedType.Global
public partial class WordCloudFunction(FunctionContext context) : BotFunction(context), ICronHandler, IFluentFunction
{
    private WordCloudOption? _option;
    private static readonly HttpClient _client = new() { Timeout = TimeSpan.FromMinutes(3) };

    public async Task OnCreatingAsync(FunctionBuilder builder, CancellationToken token)
    {
        if (_context.Configuration.Get<WordCloudOption>() is not { } option)
        {
            LogOptionBindingFailed(_context.Logger);
            return;
        }

        _option = option;

        await CreateTableAsync(token);

        builder.On<GroupMessageEvent>()
            .AsAlwaysFired()
            .Do(ctx =>
            {
                var (e, t) = ctx;
                return InsertDataAsync(
                    e.GroupId,
                    string.Join(' ', e.Message.OfType<TextData>().Select(s => s.Text)),
                    t
                );
            })
            .On<GroupMessageEvent>()
            .OnCommand("word_cloud")
            .Do(ctx =>
            {
                var (e, t) = ctx;
                return SendWordCloudAsync(e.GroupId, token: t);
            });
    }


    private async Task SendWordCloudAsync(long groupId, bool clear = false, CancellationToken token = default)
    {
        var messages = await GetGroupMessagesAsync(groupId, token);
        var content = string.Join('\n', messages);
        using var response = await _client.PostAsJsonAsync(
            _option!.ApiAddress,
            _option.CloudOption with { Text = content },
            cancellationToken: token
        );

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

        await new SendGroupMessageRequest(groupId, [
            new ImageData(url)
        ]).SendAsync(_context.OperationProvider, _context.Logger, token);
    }

    public override async Task StopAsync(CancellationToken token)
    {
        _client.Dispose();
        _semaphore.Dispose();
        await _db.DisposeAsync();
    }

    public async Task OnCronEventAsync(CancellationToken token)
    {
        try
        {
            var groups = await GetGroupsAsync(token);
            foreach (var group in groups)
            {
                await SendWordCloudAsync(group, true, token);
            }
        }
        catch (Exception e)
        {
            LogExceptionOccurred(_context.Logger, e);
        }
    }

    #region Database

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

    #endregion

    #region Log

    [LoggerMessage(Level = LogLevel.Warning, Message = "Option binding failed")]
    private static partial void LogOptionBindingFailed(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Word cloud api request failed for group {GroupId}")]
    private static partial void LogApiRequestFailed(ILogger logger, long groupId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Exception occurred while sending word cloud")]
    private static partial void LogExceptionOccurred(ILogger logger, Exception exception);

    #endregion
}
