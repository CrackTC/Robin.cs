using System.Collections.Frozen;
using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using Robin.Abstractions.Communication;
using Robin.Abstractions.Message;
using Robin.Abstractions.Message.Entities;
using Robin.Abstractions.Operation.Requests;
using Robin.Abstractions.Operation.Responses;

namespace Robin.Extensions.UserRank;

public partial class UserRankJob : IJob
{
    private readonly SqliteCommand _getGroupsCommand;

    private const string GetGroupsSql =
        "SELECT DISTINCT group_id FROM user_rank";

    private readonly SqliteCommand _getGroupPeopleCountCommand;

    private const string GetGroupPeopleCountSql =
        "SELECT COUNT(DINSTINCT user_id) FROM user_rank WHERE group_id = $group_id";

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

    private readonly ILogger<UserRankJob> _logger;

    private readonly IOperationProvider _operation;

    public UserRankJob(
        IServiceProvider service,
        IOperationProvider operation,
        SqliteConnection connection,
        UserRankOption option)
    {
        _option = option;
        _logger = service.GetRequiredService<Logger<UserRankJob>>();
        _operation = operation;
        _getGroupsCommand = connection.CreateCommand();
        _getGroupsCommand.CommandText = GetGroupsSql;
        _getGroupPeopleCountCommand = connection.CreateCommand();
        _getGroupPeopleCountCommand.CommandText = GetGroupPeopleCountSql;
        _getGroupMessageCountCommand = connection.CreateCommand();
        _getGroupMessageCountCommand.CommandText = GetGroupMessageCountSql;
        _getGroupTopNCommand = connection.CreateCommand();
        _getGroupTopNCommand.CommandText = GetGroupTopNSql;
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

    private int GetGroupPeopleCount(long groupId)
    {
        _getGroupPeopleCountCommand.Parameters.AddWithValue("$group_id", groupId);
        var count = _getGroupPeopleCountCommand.ExecuteScalar();
        _getGroupPeopleCountCommand.Parameters.Clear();
        return Convert.ToInt32(count);
    }

    private int GetGroupMessageCount(long groupId)
    {
        _getGroupMessageCountCommand.Parameters.AddWithValue("$group_id", groupId);
        var count = _getGroupMessageCountCommand.ExecuteScalar();
        _getGroupMessageCountCommand.Parameters.Clear();
        return Convert.ToInt32(count);
    }

    private IEnumerable<(long Id, int Count)> GetGroupTopN(long groupId, int n)
    {
        _getGroupTopNCommand.Parameters.AddWithValue("$group_id", groupId);
        _getGroupTopNCommand.Parameters.AddWithValue("$n", n);

        var top = new List<(long, int)>();
        using var reader = _getGroupTopNCommand.ExecuteReader();
        while (reader.Read())
        {
            top.Add((reader.GetInt64(0), reader.GetInt32(1)));
        }

        _getGroupTopNCommand.Parameters.Clear();
        return top;
    }

    private void ClearGroupMessages(long groupId)
    {
        _clearGroupMessagesCommand.Parameters.AddWithValue("$group_id", groupId);
        _clearGroupMessagesCommand.ExecuteNonQuery();
        _clearGroupMessagesCommand.Parameters.Clear();
    }

    internal async Task SendUserRankAsync(long groupId, bool clear = false, CancellationToken token = default)
    {
        var peopleCount = GetGroupPeopleCount(groupId);
        var messageCount = GetGroupMessageCount(groupId);
        var top = GetGroupTopN(groupId, _option.TopN);
        if (clear) ClearGroupMessages(groupId);

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
                .Select(member => (member.UserId, Name: member.Card ?? member.Nickname))
                .ToFrozenDictionary(pair => pair.UserId, pair => pair.Name);

            var stringBuilder = new StringBuilder($"本群 {peopleCount} 位朋友共产生 {messageCount} 条发言\n活跃用户排行榜\n");
            stringBuilder.AppendJoin('\n', top.Select(pair => $"{dict[pair.Id]} 贡献：{pair.Count}"));
            message = stringBuilder.ToString();
        }

        MessageBuilder builder = [new TextData(message)];
        if (await _operation.SendRequestAsync(new SendGroupMessageRequest(groupId, builder.Build()), token) is not
            { Success: true })
        {
            LogSendFailed(_logger, groupId);
            return;
        }

        LogUserRankSent(_logger, groupId);
    }

    public Task Execute(IJobExecutionContext context) =>
        Task.WhenAll(GetGroups().Select(group => SendUserRankAsync(group, true)));

    #region Log

    [LoggerMessage(EventId = 0, Level = LogLevel.Warning, Message = "Get group member list failed for group {GroupId}")]
    private static partial void LogGetGroupMemberListFailed(ILogger logger, long groupId);

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Send message failed for group {GroupId}")]
    private static partial void LogSendFailed(ILogger logger, long groupId);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Word cloud sent for group {GroupId}")]
    private static partial void LogUserRankSent(ILogger logger, long groupId);

    #endregion
}