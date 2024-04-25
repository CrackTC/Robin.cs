using System.Collections.Frozen;
using System.Text;
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
using Robin.Abstractions.Operation.Responses;
using Robin.Annotations.Cron;
using Robin.Annotations.Filters;
using Robin.Annotations.Filters.Message;

namespace Robin.Extensions.UserRank;

[BotFunctionInfo("user_rank", "daily user rank", typeof(GroupMessageEvent))]
[OnCommand("rank")]
[OnCron("0 0 0 * * ?")]
// ReSharper disable once UnusedType.Global
public partial class UserRankFunction : BotFunction, IFilterHandler, ICronHandler
{
    private UserRankOption? _option;
    private readonly SqliteConnection _connection;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly ILogger<UserRankFunction> _logger;
    private readonly SqliteCommand _createTableCommand;

    private const string CreateTableSql =
        "CREATE TABLE IF NOT EXISTS user_rank (group_id INTEGER NOT NULL, user_id INTEGER NOT NULL, message TEXT NOT NULL)";

    private readonly SqliteCommand _insertDataCommand;

    private const string InsertDataSql =
        "INSERT INTO user_rank (group_id, user_id, message) VALUES ($group_id, $user_id, $message)";

    private readonly SqliteCommand _getGroupsCommand;

    private const string GetGroupsSql =
        "SELECT DISTINCT group_id FROM user_rank";

    private readonly SqliteCommand _getGroupPeopleCountCommand;

    private const string GetGroupPeopleCountSql =
        "SELECT COUNT(DISTINCT user_id) FROM user_rank WHERE group_id = $group_id";

    private readonly SqliteCommand _getGroupMessageCountCommand;

    private const string GetGroupMessageCountSql =
        "SELECT COUNT(*) FROM user_rank WHERE group_id = $group_id";

    private readonly SqliteCommand _getGroupTopNCommand;

    private const string GetGroupTopNSql =
        "SELECT user_id, COUNT(*) FROM user_rank WHERE group_id = $group_id GROUP BY user_id ORDER BY COUNT(*) DESC LIMIT $n";

    private readonly SqliteCommand _clearGroupMessagesCommand;

    private const string ClearGroupMessagesSql =
        "DELETE FROM user_rank WHERE group_id = $group_id";

    public UserRankFunction(
        IServiceProvider service,
        long uin,
        IOperationProvider operation,
        IConfiguration configuration,
        IEnumerable<BotFunction> functions) : base(service, uin, operation, configuration, functions)
    {
        _connection = new SqliteConnection($"Data Source=user_rank-{uin}.db");

        _logger = service.GetRequiredService<ILogger<UserRankFunction>>();

        _createTableCommand = _connection.CreateCommand();
        _createTableCommand.CommandText = CreateTableSql;
        _insertDataCommand = _connection.CreateCommand();
        _insertDataCommand.CommandText = InsertDataSql;
        _getGroupsCommand = _connection.CreateCommand();
        _getGroupsCommand.CommandText = GetGroupsSql;
        _getGroupPeopleCountCommand = _connection.CreateCommand();
        _getGroupPeopleCountCommand.CommandText = GetGroupPeopleCountSql;
        _getGroupMessageCountCommand = _connection.CreateCommand();
        _getGroupMessageCountCommand.CommandText = GetGroupMessageCountSql;
        _getGroupTopNCommand = _connection.CreateCommand();
        _getGroupTopNCommand.CommandText = GetGroupTopNSql;
        _clearGroupMessagesCommand = _connection.CreateCommand();
        _clearGroupMessagesCommand.CommandText = ClearGroupMessagesSql;
    }

    private Task<int> CreateTableAsync(CancellationToken token) => _createTableCommand.ExecuteNonQueryAsync(token);

    private async Task InsertDataAsync(long groupId, long userId, string message, CancellationToken token)
    {
        await _semaphore.WaitAsync(token);
        try
        {
            _insertDataCommand.Parameters.AddWithValue("$group_id", groupId);
            _insertDataCommand.Parameters.AddWithValue("$user_id", userId);
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

    private async Task<int> GetGroupPeopleCountAsync(long groupId, CancellationToken token)
    {
        object? count;

        await _semaphore.WaitAsync(token);
        try
        {
            _getGroupPeopleCountCommand.Parameters.AddWithValue("$group_id", groupId);
            count = await _getGroupPeopleCountCommand.ExecuteScalarAsync(token);
        }
        finally
        {
            _getGroupPeopleCountCommand.Parameters.Clear();
            _semaphore.Release();
        }

        return Convert.ToInt32(count);
    }

    private async Task<int> GetGroupMessageCountAsync(long groupId, CancellationToken token)
    {
        object? count;

        await _semaphore.WaitAsync(token);
        try
        {
            _getGroupMessageCountCommand.Parameters.AddWithValue("$group_id", groupId);
            count = await _getGroupMessageCountCommand.ExecuteScalarAsync(token);
        }
        finally
        {
            _getGroupMessageCountCommand.Parameters.Clear();
            _semaphore.Release();
        }

        return Convert.ToInt32(count);
    }

    private async Task<IEnumerable<(long Id, int Count)>> GetGroupTopNAsync(long groupId, int n,
        CancellationToken token)
    {
        var top = new List<(long, int)>();

        await _semaphore.WaitAsync(token);
        try
        {
            _getGroupTopNCommand.Parameters.AddWithValue("$group_id", groupId);
            _getGroupTopNCommand.Parameters.AddWithValue("$n", n);
            await using var reader = await _getGroupTopNCommand.ExecuteReaderAsync(token);
            while (await reader.ReadAsync(token))
            {
                top.Add((reader.GetInt64(0), reader.GetInt32(1)));
            }
        }
        finally
        {
            _getGroupTopNCommand.Parameters.Clear();
            _semaphore.Release();
        }

        return top;
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

    private async Task SendUserRankAsync(long groupId, bool clear = false, CancellationToken token = default)
    {
        var peopleCount = await GetGroupPeopleCountAsync(groupId, token);
        var messageCount = await GetGroupMessageCountAsync(groupId, token);
        var top = await GetGroupTopNAsync(groupId, _option!.TopN, token);
        if (clear) await ClearGroupMessagesAsync(groupId, token);

        string message;

        if (peopleCount == 0 || messageCount == 0) message = "本群暂无发言记录";
        else
        {
            if (await _operation.SendRequestAsync(new GetGroupMemberListRequest(groupId, true), token) is not
                GetGroupMemberListResponse { Success: true, Members: not null } memberList)
            {
                LogGetGroupMemberListFailed(_logger, groupId);
                return;
            }

            var dict = memberList.Members
                .Select(member => (member.UserId,
                    Name: string.IsNullOrEmpty(member.Card) ? member.Nickname : member.Card))
                .ToFrozenDictionary(pair => pair.UserId, pair => pair.Name);

            var stringBuilder = new StringBuilder($"本群 {peopleCount} 位朋友共产生 {messageCount} 条发言\n活跃用户排行榜\n");
            stringBuilder.AppendJoin('\n', top.Select(pair => $"{(dict.TryGetValue(pair.Id, out var value) ? value : pair.Id)} 贡献：{pair.Count}"));
            message = stringBuilder.ToString();
        }

        if (await _operation.SendRequestAsync(new SendGroupMessageRequest(groupId, [new TextData(message)]), token) is
            not { Success: true })
        {
            LogSendFailed(_logger, groupId);
            return;
        }

        LogUserRankSent(_logger, groupId);
    }

    public override async Task OnEventAsync(long selfId, BotEvent @event, CancellationToken token)
    {
        if (@event is not GroupMessageEvent e) return;
        await InsertDataAsync(e.GroupId, e.UserId,
            string.Join(' ', e.Message.OfType<TextData>().Select(s => s.Text)), token);
    }

    public override async Task StartAsync(CancellationToken token)
    {
        if (_configuration.Get<UserRankOption>() is not { } option)
        {
            LogOptionBindingFailed(_logger);
            return;
        }

        _option = option;

        await _connection.OpenAsync(token);

        await _createTableCommand.PrepareAsync(token);
        await _insertDataCommand.PrepareAsync(token);
        await _getGroupsCommand.PrepareAsync(token);
        await _getGroupPeopleCountCommand.PrepareAsync(token);
        await _getGroupMessageCountCommand.PrepareAsync(token);
        await _getGroupTopNCommand.PrepareAsync(token);
        await _clearGroupMessagesCommand.PrepareAsync(token);
        await CreateTableAsync(token);
    }

    public override async Task StopAsync(CancellationToken token)
    {
        await _connection.CloseAsync();
        _semaphore.Dispose();
    }

    public async Task OnCronEventAsync(CancellationToken token)
    {
        try
        {
            await Task.WhenAll((await GetGroupsAsync(token)).Select(group => SendUserRankAsync(group, true, token)));
        }
        catch (Exception e)
        {
            LogExceptionOccurred(_logger, e);
        }
    }

    public async Task<bool> OnFilteredEventAsync(int filterGroup, long selfId, BotEvent @event, CancellationToken token)
    {
        if (@event is not GroupMessageEvent e) return false;

        await SendUserRankAsync(e.GroupId, token: token);
        return true;
    }

    #region Log

    [LoggerMessage(EventId = 0, Level = LogLevel.Warning, Message = "Option binding failed")]
    private static partial void LogOptionBindingFailed(ILogger logger);

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Get group member list failed for group {GroupId}")]
    private static partial void LogGetGroupMemberListFailed(ILogger logger, long groupId);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Send message failed for group {GroupId}")]
    private static partial void LogSendFailed(ILogger logger, long groupId);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "Word cloud sent for group {GroupId}")]
    private static partial void LogUserRankSent(ILogger logger, long groupId);

    [LoggerMessage(EventId = 4, Level = LogLevel.Warning, Message = "Exception occurred while sending word cloud")]
    private static partial void LogExceptionOccurred(ILogger logger, Exception exception);

    #endregion
}