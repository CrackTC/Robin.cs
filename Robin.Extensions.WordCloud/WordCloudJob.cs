using System.Net.Http.Json;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using Robin.Abstractions.Communication;
using Robin.Abstractions.Message.Entities;
using Robin.Abstractions.Operation.Requests;

namespace Robin.Extensions.WordCloud;

public partial class WordCloudJob : IJob
{
    private readonly SqliteCommand _getGroupsCommand;

    private const string GetGroupsSql =
        "SELECT DISTINCT group_id FROM word_cloud";

    private readonly SqliteCommand _getGroupMessagesCommand;

    private const string GetGroupMessagesSql =
        "SELECT message FROM word_cloud WHERE group_id = $group_id";

    private readonly SqliteCommand _clearGroupMessagesCommand;

    private const string ClearGroupMessagesSql =
        "DELETE FROM word_cloud WHERE group_id = $group_id";

    private static readonly HttpClient _client = new();
    private readonly WordCloudOption _option;
    private readonly ILogger<WordCloudJob> _logger;
    private readonly IOperationProvider _operation;
    private readonly SemaphoreSlim _semaphore;

    public WordCloudJob(
        IServiceProvider service,
        IOperationProvider operation,
        SqliteConnection connection,
        SemaphoreSlim semaphore,
        WordCloudOption option)
    {
        _option = option;
        _logger = service.GetRequiredService<ILogger<WordCloudJob>>();
        _operation = operation;
        _semaphore = semaphore;
        _getGroupsCommand = connection.CreateCommand();
        _getGroupsCommand.CommandText = GetGroupsSql;
        _getGroupsCommand.Prepare();
        _getGroupMessagesCommand = connection.CreateCommand();
        _getGroupMessagesCommand.CommandText = GetGroupMessagesSql;
        _clearGroupMessagesCommand = connection.CreateCommand();
        _clearGroupMessagesCommand.CommandText = ClearGroupMessagesSql;
    }

    private async Task<IEnumerable<long>> GetGroupsAsync(CancellationToken token = default)
    {
        var groups = new List<long>();

        try
        {
            await _semaphore.WaitAsync(token);
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
        _getGroupMessagesCommand.Parameters.AddWithValue("$group_id", groupId);
        var messages = new List<string>();

        try
        {
            await _semaphore.WaitAsync(token);
            await using var reader = await _getGroupMessagesCommand.ExecuteReaderAsync(token);
            while (await reader.ReadAsync(token))
            {
                messages.Add(reader.GetString(0));
            }
        }
        finally
        {
            _semaphore.Release();
        }

        _getGroupMessagesCommand.Parameters.Clear();
        return messages;
    }

    private async Task ClearGroupMessagesAsync(long groupId, CancellationToken token)
    {
        _clearGroupMessagesCommand.Parameters.AddWithValue("$group_id", groupId);

        try
        {
            await _clearGroupMessagesCommand.ExecuteNonQueryAsync(token);
        }
        finally
        {
            _semaphore.Release();
        }

        _clearGroupMessagesCommand.Parameters.Clear();
    }

    internal async Task SendWordCloudAsync(long groupId, bool clear = false, CancellationToken token = default)
    {
        var messages = await GetGroupMessagesAsync(groupId, token);
        var content = string.Join('\n', messages);
        using var response = await _client.PostAsJsonAsync(_option.ApiAddress,
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

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            await Task.WhenAll((await GetGroupsAsync()).Select(group => SendWordCloudAsync(group, true)));
        }
        catch (Exception e)
        {
            LogExceptionOccurred(_logger, e);
        }
    }

    #region Log

    [LoggerMessage(EventId = 0, Level = LogLevel.Warning,
        Message = "Word cloud api request failed for group {GroupId}")]
    private static partial void LogApiRequestFailed(ILogger logger, long groupId);

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Send message failed for group {GroupId}")]
    private static partial void LogSendFailed(ILogger logger, long groupId);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Word cloud sent for group {GroupId}")]
    private static partial void LogWordCloudSent(ILogger logger, long groupId);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "Exception occurred while sending word cloud")]
    private static partial void LogExceptionOccurred(ILogger logger, Exception exception);

    #endregion
}