using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Robin.Abstractions;
using Robin.Abstractions.Context;
using Robin.Abstractions.Event.Message;
using Robin.Abstractions.Message.Entity;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Requests;
using Robin.Abstractions.Utility;
using Robin.Middlewares.Fluent;
using Robin.Middlewares.Fluent.Event;

namespace Robin.Extensions.WordCloud;

[BotFunctionInfo("word_cloud", "群词云生成")]
public partial class WordCloudFunction(
    FunctionContext<WordCloudOption> context
) : BotFunction<WordCloudOption>(context), IFluentFunction
{
    private static readonly HttpClient _client = new() { Timeout = TimeSpan.FromMinutes(3) };

    public async Task OnCreatingAsync(FunctionBuilder builder, CancellationToken token)
    {
        await CreateTableAsync(token);

        builder.On<GroupMessageEvent>("collect message")
            .AsIntrinsic()
            .Do(ctx => InsertDataAsync(
                ctx.Event.GroupId,
                string.Join(' ', ctx.Event.Message.OfType<TextData>().Select(s => s.Text)),
                ctx.Token
            ))

            .On<GroupMessageEvent>("show word cloud")
            .OnCommand("word_cloud")
            .DoExpensive(ctx => SendWordCloudAsync(ctx.Event.GroupId, token: ctx.Token), ctx => ctx, _context)

            .OnCron("0 0 0 * * ?", "word cloud cron")
            .Do(async token =>
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
            });
    }


    private async Task<bool> SendWordCloudAsync(long groupId, bool clear = false, CancellationToken token = default)
    {
        var messages = await GetGroupMessagesAsync(groupId, token);
        var content = string.Join('\n', messages);
        using var response = await _client.PostAsJsonAsync(
            _context.Configuration.ApiAddress,
            _context.Configuration.CloudOption with { Text = content },
            cancellationToken: token
        );

        if (!response.IsSuccessStatusCode)
        {
            LogApiRequestFailed(_context.Logger, groupId);
            return false;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(token);
        var url = (await JsonNode.ParseAsync(stream, cancellationToken: token))?["url"]?.ToString();

        if (url is null)
        {
            LogApiRequestFailed(_context.Logger, groupId);
            return false;
        }

        if (clear) await ClearGroupMessagesAsync(groupId, token);

        return await new SendGroupMessageRequest(groupId, [new ImageData(url)]).SendAsync(_context, token)
            is { Success: true };
    }

    public override async Task StopAsync(CancellationToken token)
    {
        _client.Dispose();
        _semaphore.Dispose();
        await _db.DisposeAsync();
    }

    #region Database

    private readonly WordCloudDbContext _db = new(context.BotContext.Uin);
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private Task<bool> CreateTableAsync(CancellationToken token) =>
        _semaphore.ConsumeAsync(() => _db.Database.EnsureCreatedAsync(token), token);

    private Task InsertDataAsync(long groupId, string message, CancellationToken token) =>
        _semaphore.ConsumeAsync(() =>
        {
            _db.Records.Add(new Record { GroupId = groupId, Content = message });
            return _db.SaveChangesAsync(token);
        }, token);

    private Task<List<long>> GetGroupsAsync(CancellationToken token) =>
        _semaphore.ConsumeAsync(() => _db.Records
            .Select(r => r.GroupId)
            .Distinct()
            .ToListAsync(token), token);

    private Task<List<string>> GetGroupMessagesAsync(long groupId, CancellationToken token) =>
        _semaphore.ConsumeAsync(() => _db.Records
            .Where(r => r.GroupId == groupId)
            .Select(r => r.Content)
            .ToListAsync(token), token);

    private async Task ClearGroupMessagesAsync(long groupId, CancellationToken token) =>
        await _semaphore.ConsumeAsync(() =>
        {
            _db.Records.RemoveRange(_db.Records.Where(r => r.GroupId == groupId));
            return _db.SaveChangesAsync(token);
        }, token);

    #endregion

    #region Log

    [LoggerMessage(Level = LogLevel.Warning, Message = "Word cloud api request failed for group {GroupId}")]
    private static partial void LogApiRequestFailed(ILogger logger, long groupId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Exception occurred while sending word cloud")]
    private static partial void LogExceptionOccurred(ILogger logger, Exception exception);

    #endregion
}
