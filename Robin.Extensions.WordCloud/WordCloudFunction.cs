using System.Net.Http.Json;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Robin.Abstractions;
using Robin.Abstractions.Communication;
using Robin.Abstractions.Event;
using Robin.Abstractions.Event.Message;
using Robin.Abstractions.Message.Entities;
using Robin.Abstractions.Operation.Requests;
using Robin.Annotations.Cron;
using Robin.Annotations.Filters;
using Robin.Annotations.Filters.Message;

namespace Robin.Extensions.WordCloud;

[BotFunctionInfo("word_cloud", "daily word cloud", typeof(GroupMessageEvent))]
[OnCommand("word_cloud")]
[OnCron("0 0 0 * * ?")]
// ReSharper disable once UnusedType.Global
public partial class WordCloudFunction : BotFunction, IFilterHandler, ICronHandler
{
    private WordCloudOption? _option;
    private readonly SqliteConnection _connection;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly ILogger<WordCloudFunction> _logger;
    private readonly SqliteCommand _createTableCommand;
    private static readonly HttpClient _client = new();

    private const string CreateTableSql =
        "CREATE TABLE IF NOT EXISTS word_cloud (group_id INTEGER NOT NULL, message TEXT NOT NULL)";

    private readonly SqliteCommand _insertDataCommand;

    private const string InsertDataSql =
        "INSERT INTO word_cloud (group_id, message) VALUES ($group_id, $message)";

    private readonly SqliteCommand _getGroupsCommand;

    private const string GetGroupsSql =
        "SELECT DISTINCT group_id FROM word_cloud";

    private readonly SqliteCommand _getGroupMessagesCommand;

    private const string GetGroupMessagesSql =
        "SELECT message FROM word_cloud WHERE group_id = $group_id";

    private readonly SqliteCommand _clearGroupMessagesCommand;

    private const string ClearGroupMessagesSql =
        "DELETE FROM word_cloud WHERE group_id = $group_id";

    public WordCloudFunction(IServiceProvider service,
        long uin,
        IOperationProvider operation,
        IConfiguration configuration,
        IEnumerable<BotFunction> functions) : base(service, uin, operation, configuration, functions)
    {
        _connection = new SqliteConnection($"Data Source=word_cloud-{uin}.db");

        _logger = service.GetRequiredService<ILogger<WordCloudFunction>>();

        _createTableCommand = _connection.CreateCommand();
        _createTableCommand.CommandText = CreateTableSql;
        _insertDataCommand = _connection.CreateCommand();
        _insertDataCommand.CommandText = InsertDataSql;
        _getGroupsCommand = _connection.CreateCommand();
        _getGroupsCommand.CommandText = GetGroupsSql;
        _getGroupMessagesCommand = _connection.CreateCommand();
        _getGroupMessagesCommand.CommandText = GetGroupMessagesSql;
        _clearGroupMessagesCommand = _connection.CreateCommand();
        _clearGroupMessagesCommand.CommandText = ClearGroupMessagesSql;
    }

    private Task<int> CreateTableAsync(CancellationToken token) => _createTableCommand.ExecuteNonQueryAsync(token);

    private async Task InsertDataAsync(long groupId, string message, CancellationToken token)
    {
        await _semaphore.WaitAsync(token);
        try
        {
            _insertDataCommand.Parameters.AddWithValue("$group_id", groupId);
            _insertDataCommand.Parameters.AddWithValue("$message", message);
            await _insertDataCommand.ExecuteNonQueryAsync(token);
        }
        finally
        {
            _insertDataCommand.Parameters.Clear();
            _semaphore.Release();
        }
    }

    private async Task<IEnumerable<long>> GetGroupsAsync(CancellationToken token = default)
    {
        var groups = new List<long>();

        await _semaphore.WaitAsync(token);
        try
        {
            await using var reader = await _getGroupsCommand.ExecuteReaderAsync(token);
            while (await reader.ReadAsync(token))
            {
                groups.Add(reader.GetInt64(0));
            }
        }
        finally
        {
            _semaphore.Release();
        }

        return groups;
    }

    private async Task<IEnumerable<string>> GetGroupMessagesAsync(long groupId, CancellationToken token)
    {
        var messages = new List<string>();

        await _semaphore.WaitAsync(token);
        try
        {
            _getGroupMessagesCommand.Parameters.AddWithValue("$group_id", groupId);
            await using var reader = await _getGroupMessagesCommand.ExecuteReaderAsync(token);
            while (await reader.ReadAsync(token))
            {
                messages.Add(reader.GetString(0));
            }
        }
        finally
        {
            _getGroupMessagesCommand.Parameters.Clear();
            _semaphore.Release();
        }

        return messages;
    }

    private async Task ClearGroupMessagesAsync(long groupId, CancellationToken token)
    {
        await _semaphore.WaitAsync(token);
        try
        {
            _clearGroupMessagesCommand.Parameters.AddWithValue("$group_id", groupId);
            await _clearGroupMessagesCommand.ExecuteNonQueryAsync(token);
        }
        finally
        {
            _clearGroupMessagesCommand.Parameters.Clear();
            _semaphore.Release();
        }
    }

    private async Task SendWordCloudAsync(long groupId, bool clear = false, CancellationToken token = default)
    {
        var messages = await GetGroupMessagesAsync(groupId, token);
        var content = string.Join('\n', messages);
        using var response = await _client.PostAsJsonAsync(_option!.ApiAddress,
            _option.CloudOption with { Text = content }, cancellationToken: token);
        if (!response.IsSuccessStatusCode)
        {
            LogApiRequestFailed(_logger, groupId);
            return;
        }

        if (clear) await ClearGroupMessagesAsync(groupId, token);

        var base64 = Convert.ToBase64String(await response.Content.ReadAsByteArrayAsync(token));

        if (await _operation.SendRequestAsync(
                new SendGroupMessageRequest(groupId, [new ImageData($"base64://{base64}")]), token) is not
                { Success: true })
        {
            LogSendFailed(_logger, groupId);
            return;
        }

        LogWordCloudSent(_logger, groupId);
    }

    public override async Task OnEventAsync(long selfId, BotEvent @event, CancellationToken token)
    {
        if (@event is not GroupMessageEvent e) return;

        await InsertDataAsync(e.GroupId,
            string.Join(' ', e.Message.OfType<TextData>().Select(s => s.Text)), token);
    }

    public override async Task StartAsync(CancellationToken token)
    {
        if (_configuration.Get<WordCloudOption>() is not { } option)
        {
            LogOptionBindingFailed(_logger);
            return;
        }

        _option = option;
        if (!string.IsNullOrEmpty(_option.BackgroundImagePath))
        {
            var bytes = await File.ReadAllBytesAsync(_option.BackgroundImagePath, token);
            _option.CloudOption.BackgroundImage = Convert.ToBase64String(bytes);
        }

        await _connection.OpenAsync(token);

        await _createTableCommand.PrepareAsync(token);
        await _insertDataCommand.PrepareAsync(token);
        await _getGroupsCommand.PrepareAsync(token);
        await _getGroupMessagesCommand.PrepareAsync(token);
        await _clearGroupMessagesCommand.PrepareAsync(token);
        await CreateTableAsync(token);
    }

    public override async Task StopAsync(CancellationToken token)
    {
        await _connection.CloseAsync();
    }

    public Task OnFilteredEventAsync(int filterGroup, long selfId, BotEvent @event, CancellationToken token) =>
        @event is GroupMessageEvent e ? SendWordCloudAsync(e.GroupId, token: token) : Task.CompletedTask;

    public async Task OnCronEventAsync(CancellationToken token)
    {
        try
        {
            await Task.WhenAll((await GetGroupsAsync(token)).Select(group => SendWordCloudAsync(group, true, token)));
        }
        catch (Exception e)
        {
            LogExceptionOccurred(_logger, e);
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
