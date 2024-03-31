using System.Net.Http.Json;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using Robin.Abstractions.Communication;
using Robin.Abstractions.Message;
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

    private readonly HttpClient _client = new();
    
    private readonly WordCloudOption _option;
    
    private readonly ILogger<WordCloudJob> _logger;

    private readonly IOperationProvider _operation;

    public WordCloudJob(
        IServiceProvider service,
        IOperationProvider operation,
        SqliteConnection connection,
        WordCloudOption option)
    {
        _option = option;
        _logger = service.GetRequiredService<Logger<WordCloudJob>>();
        _operation = operation;
        _getGroupsCommand = connection.CreateCommand();
        _getGroupsCommand.CommandText = GetGroupsSql;
        _getGroupMessagesCommand = connection.CreateCommand();
        _getGroupMessagesCommand.CommandText = GetGroupMessagesSql;
        _clearGroupMessagesCommand = connection.CreateCommand();
        _clearGroupMessagesCommand.CommandText = ClearGroupMessagesSql;
    }

    private List<long> GetGroups()
    {
        var groups = new List<long>();
        using var reader = _getGroupsCommand.ExecuteReader();
        while (reader.Read())
        {
            groups.Add(reader.GetInt64(0));
        }

        return groups;
    }

    private List<string> GetGroupMessages(long groupId)
    {
        _getGroupMessagesCommand.Parameters.AddWithValue("$group_id", groupId);

        var messages = new List<string>();
        using var reader = _getGroupMessagesCommand.ExecuteReader();
        while (reader.Read())
        {
            messages.Add(reader.GetString(0));
        }

        _getGroupMessagesCommand.Parameters.Clear();
        return messages;
    }

    private void ClearGroupMessages(long groupId)
    {
        _clearGroupMessagesCommand.Parameters.AddWithValue("$group_id", groupId);
        _clearGroupMessagesCommand.ExecuteNonQuery();
        _clearGroupMessagesCommand.Parameters.Clear();
    }

    public async Task Execute(IJobExecutionContext context)
    {
        foreach (var group in GetGroups())
        {
            var messages = GetGroupMessages(group);
            var content = string.Join('\n', messages);
            using var response = await _client.PostAsJsonAsync(_option.ApiAddress, _option.CloudOption with { Text = content });
            if (!response.IsSuccessStatusCode)
            {
                LogApiRequestFailed(_logger, group);
                continue;
            }
            
            ClearGroupMessages(group);

            var base64 = Convert.ToBase64String(await response.Content.ReadAsByteArrayAsync());
            
            var builder = new MessageBuilder();
            builder.Add(new ImageData($"base64://{base64}"));
            var response1 = await _operation.SendRequestAsync(new SendGroupMessageRequest(group, builder.Build()));
            if (!(response1?.Success ?? false))
            {
                LogSendFailed(_logger, group);
                continue;
            }
            
            LogWordCloudSent(_logger, group);
        }
    }

    #region Log

    [LoggerMessage(EventId = 0, Level = LogLevel.Warning, Message = "Word cloud api request failed for group {GroupId}")]
    private static partial void LogApiRequestFailed(ILogger logger, long groupId);
    
    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Send message failed for group {GroupId}")]
    private static partial void LogSendFailed(ILogger logger, long groupId);
    
    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Word cloud sent for group {GroupId}")]
    private static partial void LogWordCloudSent(ILogger logger, long groupId);

    #endregion
}