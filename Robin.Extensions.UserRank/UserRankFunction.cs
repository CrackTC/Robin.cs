using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl;
using Robin.Abstractions;
using Robin.Abstractions.Communication;
using Robin.Abstractions.Event;
using Robin.Abstractions.Event.Message;
using Robin.Abstractions.Message.Entities;

namespace Robin.Extensions.UserRank;

[BotFunctionInfo("user_rank", "daily user rank", typeof(GroupMessageEvent))]
public partial class UserRankFunction : BotFunction
{
    private IScheduler? _scheduler;
    private UserRankOption? _option;
    private readonly SqliteConnection _connection = new("Data Source=user_rank.db");
    private readonly Logger<UserRankFunction> _logger;
    private readonly SqliteCommand _createTableCommand;

    private const string CreateTableSql =
        "CREATE TABLE IF NOT EXISTS user_rank (group_id INTEGER NOT NULL, user_id INTEGER NOT NULL, message TEXT NOT NULL)";

    private readonly SqliteCommand _insertDataCommand;

    private const string InsertDataSql =
        "INSERT INTO user_rank (group_id, user_id, message) VALUES ($group_id, $user_id, $message)";

    public UserRankFunction(
        IServiceProvider service,
        IOperationProvider operation,
        IConfiguration configuration) : base(service, operation, configuration)
    {
        _logger = service.GetRequiredService<Logger<UserRankFunction>>();
        _createTableCommand = _connection.CreateCommand();
        _createTableCommand.CommandText = CreateTableSql;
        _insertDataCommand = _connection.CreateCommand();
        _insertDataCommand.CommandText = InsertDataSql;
    }

    private void CreateTable() => _createTableCommand.ExecuteNonQuery();

    private void InsertData(long groupId, long userId, string message)
    {
        _insertDataCommand.Parameters.AddWithValue("$group_id", groupId);
        _insertDataCommand.Parameters.AddWithValue("$user_id", userId);
        _insertDataCommand.Parameters.AddWithValue("$message", message);
        _insertDataCommand.ExecuteNonQuery();
        _insertDataCommand.Parameters.Clear();
    }

    public override void OnEvent(long selfId, BotEvent @event)
    {
        if (@event is not GroupMessageEvent e) return;
        InsertData(e.GroupId, e.UserId,
            string.Join(' ', e.Message.Segments.Where(s => s is TextData).Select(s => (s as TextData)!.Text)));
    }

    public override async Task StartAsync(CancellationToken token)
    {
        if (_configuration.Get<UserRankOption>() is not { } option)
        {
            LogOptionBindingFailed(_logger);
            return;
        }

        _option = option;

        CreateTable();

        _scheduler = await StdSchedulerFactory.GetDefaultScheduler(token);
        _scheduler.JobFactory = new UserRankJobFactory(_service, _provider, _connection, _option);

        var job = JobBuilder.Create<UserRankJob>()
            .WithIdentity("UserRankJob", "UserRank")
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity("UserRankTrigger", "UserRank")
            .WithCronSchedule(_option.Cron)
            .Build();

        await _scheduler.ScheduleJob(job, trigger, token);
        await _scheduler.Start(token);

        LogUserRankJobScheduled(_logger);
    }

    public override Task StopAsync(CancellationToken token)
        => _scheduler?.Shutdown(token) ?? Task.CompletedTask;

    #region Log

    [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "User rank job scheduled")]
    private static partial void LogUserRankJobScheduled(ILogger logger);

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Option binding failed")]
    private static partial void LogOptionBindingFailed(ILogger logger);

    #endregion
}