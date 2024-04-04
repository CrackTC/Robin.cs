using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using Robin.Abstractions.Communication;
using Robin.Abstractions.Message.Entities;
using Robin.Abstractions.Operation.Requests;
using Robin.Abstractions.Operation.Responses;
using System.Collections.Frozen;
using System.Text;

namespace Robin.Extensions.UserRank;

public partial class UserRankJob : IJob
{
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

    private readonly UserRankOption _option;
    private readonly SemaphoreSlim _semaphore;
    private readonly ILogger<UserRankJob> _logger;
    private readonly IOperationProvider _operation;

    public UserRankJob(
        IServiceProvider service,
        IOperationProvider operation,
        SqliteConnection connection,
        SemaphoreSlim semaphore,
        UserRankOption option)
    {
        _option = option;
        _logger = service.GetRequiredService<ILogger<UserRankJob>>();
        _semaphore = semaphore;
        _operation = operation;
        _getGroupsCommand = connection.CreateCommand();
        _getGroupsCommand.CommandText = GetGroupsSql;
        _getGroupsCommand.Prepare();
        _getGroupPeopleCountCommand = connection.CreateCommand();
        _getGroupPeopleCountCommand.CommandText = GetGroupPeopleCountSql;
        _getGroupMessageCountCommand = connection.CreateCommand();
        _getGroupMessageCountCommand.CommandText = GetGroupMessageCountSql;
        _getGroupTopNCommand = connection.CreateCommand();
        _getGroupTopNCommand.CommandText = GetGroupTopNSql;
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

    private async Task<int> GetGroupPeopleCountAsync(long groupId, CancellationToken token)
    {
        _getGroupPeopleCountCommand.Parameters.AddWithValue("$group_id", groupId);
        object? count;

        try
        {
            await _semaphore.WaitAsync(token);
            count = await _getGroupPeopleCountCommand.ExecuteScalarAsync(token);
        }
        finally
        {
            _semaphore.Release();
        }

        _getGroupPeopleCountCommand.Parameters.Clear();
        return Convert.ToInt32(count);
    }

    private async Task<int> GetGroupMessageCountAsync(long groupId, CancellationToken token)
    {
        _getGroupMessageCountCommand.Parameters.AddWithValue("$group_id", groupId);
        object? count;

        try
        {
            await _semaphore.WaitAsync(token);
            count = await _getGroupMessageCountCommand.ExecuteScalarAsync(token);
        }
        finally
        {
            _semaphore.Release();
        }

        _getGroupMessageCountCommand.Parameters.Clear();
        return Convert.ToInt32(count);
    }

    private async Task<IEnumerable<(long Id, int Count)>> GetGroupTopNAsync(long groupId, int n,
        CancellationToken token)
    {
        _getGroupTopNCommand.Parameters.AddWithValue("$group_id", groupId);
        _getGroupTopNCommand.Parameters.AddWithValue("$n", n);

        var top = new List<(long, int)>();

        try
        {
            await _semaphore.WaitAsync(token);
            await using var reader = await _getGroupTopNCommand.ExecuteReaderAsync(token);
            while (await reader.ReadAsync(token))
            {
                top.Add((reader.GetInt64(0), reader.GetInt32(1)));
            }
        }
        finally
        {
            _semaphore.Release();
        }

        _getGroupTopNCommand.Parameters.Clear();
        return top;
    }

    private async Task ClearGroupMessagesAsync(long groupId, CancellationToken token)
    {
        _clearGroupMessagesCommand.Parameters.AddWithValue("$group_id", groupId);

        try
        {
            await _semaphore.WaitAsync(token);
            await _clearGroupMessagesCommand.ExecuteNonQueryAsync(token);
        }
        finally
        {
            _semaphore.Release();
        }

        _clearGroupMessagesCommand.Parameters.Clear();
    }

    internal async Task SendUserRankAsync(long groupId, bool clear = false, CancellationToken token = default)
    {
        var peopleCount = await GetGroupPeopleCountAsync(groupId, token);
        var messageCount = await GetGroupMessageCountAsync(groupId, token);
        var top = await GetGroupTopNAsync(groupId, _option.TopN, token);
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
            stringBuilder.AppendJoin('\n', top.Select(pair => $"{dict[pair.Id]} 贡献：{pair.Count}"));
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

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            await Task.WhenAll((await GetGroupsAsync()).Select(group => SendUserRankAsync(group, true)));
        }
        catch (Exception e)
        {
            LogExceptionOccurred(_logger, e);
        }
    }

    #region Log

    [LoggerMessage(EventId = 0, Level = LogLevel.Warning, Message = "Get group member list failed for group {GroupId}")]
    private static partial void LogGetGroupMemberListFailed(ILogger logger, long groupId);

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Send message failed for group {GroupId}")]
    private static partial void LogSendFailed(ILogger logger, long groupId);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Word cloud sent for group {GroupId}")]
    private static partial void LogUserRankSent(ILogger logger, long groupId);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "Exception occurred while sending word cloud")]
    private static partial void LogExceptionOccurred(ILogger logger, Exception exception);

    #endregion
}