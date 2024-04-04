using System.Collections.Specialized;
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
using Robin.Annotations.Command;

namespace Robin.Extensions.UserRank;

[BotFunctionInfo("user_rank", "daily user rank", typeof(GroupMessageEvent))]
[OnCommand("rank")]
// ReSharper disable once UnusedType.Global
public partial class UserRankFunction : BotFunction, ICommandHandler
{
    private IScheduler? _scheduler;
    private UserRankJob? _job;
    private UserRankOption? _option;
    private readonly SqliteConnection _connection;
    private readonly ILogger<UserRankFunction> _logger;
    private readonly SqliteCommand _createTableCommand;

    private const string CreateTableSql =
        "CREATE TABLE IF NOT EXISTS user_rank (group_id INTEGER NOT NULL, user_id INTEGER NOT NULL, message TEXT NOT NULL)";

    private readonly SqliteCommand _insertDataCommand;

    private const string InsertDataSql =
        "INSERT INTO user_rank (group_id, user_id, message) VALUES ($group_id, $user_id, $message)";

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
    }

    private Task<int> CreateTableAsync(CancellationToken token) => _createTableCommand.ExecuteNonQueryAsync(token);

    private async Task InsertDataAsync(long groupId, long userId, string message, CancellationToken token)
    {
        _insertDataCommand.Parameters.AddWithValue("$group_id", groupId);
        _insertDataCommand.Parameters.AddWithValue("$user_id", userId);
        _insertDataCommand.Parameters.AddWithValue("$message", message);
        await _insertDataCommand.ExecuteNonQueryAsync(token);
        _insertDataCommand.Parameters.Clear();
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
        await CreateTableAsync(token);

        _job = new UserRankJob(_service, _operation, _connection, _option);

        var properties = new NameValueCollection
        {
            [StdSchedulerFactory.PropertySchedulerInstanceName] = "UserRankScheduler"
        };
        _scheduler = await new StdSchedulerFactory(properties).GetScheduler(token);
        _scheduler.JobFactory = new UserRankJobFactory(_job);

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

    public override async Task StopAsync(CancellationToken token)
    {
        await (_scheduler?.Shutdown(token) ?? Task.CompletedTask);
        await _connection.CloseAsync();
    }

    public Task OnCommandAsync(long selfId, MessageEvent @event, CancellationToken token) =>
        @event is GroupMessageEvent e ? _job!.SendUserRankAsync(e.GroupId, token: token) : Task.CompletedTask;

    #region Log

    [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "User rank job scheduled")]
    private static partial void LogUserRankJobScheduled(ILogger logger);

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Option binding failed")]
    private static partial void LogOptionBindingFailed(ILogger logger);

    #endregion
}