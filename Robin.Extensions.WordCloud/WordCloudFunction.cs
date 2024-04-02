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

namespace Robin.Extensions.WordCloud;

[BotFunctionInfo("word_cloud", "daily word cloud", typeof(GroupMessageEvent))]
[OnCommand("word_cloud")]
// ReSharper disable once UnusedType.Global
public partial class WordCloudFunction : BotFunction, ICommandHandler
{
    private IScheduler? _scheduler;
    private WordCloudJob? _job;
    private WordCloudOption? _option;
    private readonly SqliteConnection _connection;
    private readonly Logger<WordCloudFunction> _logger;
    private readonly SqliteCommand _createTableCommand;

    private const string CreateTableSql =
        "CREATE TABLE IF NOT EXISTS word_cloud (group_id INTEGER NOT NULL, message TEXT NOT NULL)";

    private readonly SqliteCommand _insertDataCommand;

    private const string InsertDataSql =
        "INSERT INTO word_cloud (group_id, message) VALUES ($group_id, $message)";

    public WordCloudFunction(IServiceProvider service,
        long uin,
        IOperationProvider operation,
        IConfiguration configuration,
        IEnumerable<BotFunction> functions) : base(service, uin, operation, configuration, functions)
    {
        _connection = new SqliteConnection($"Data Source=word_cloud-{uin}.db");

        _logger = service.GetRequiredService<Logger<WordCloudFunction>>();

        _createTableCommand = _connection.CreateCommand();
        _createTableCommand.CommandText = CreateTableSql;
        _insertDataCommand = _connection.CreateCommand();
        _insertDataCommand.CommandText = InsertDataSql;
    }

    private Task CreateTableAsync(CancellationToken token) => _createTableCommand.ExecuteNonQueryAsync(token);

    private async Task InsertDataAsync(long groupId, string message, CancellationToken token)
    {
        _insertDataCommand.Parameters.AddWithValue("$group_id", groupId);
        _insertDataCommand.Parameters.AddWithValue("$message", message);
        await _insertDataCommand.ExecuteNonQueryAsync(token);
        _insertDataCommand.Parameters.Clear();
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

        await _connection.OpenAsync(token);
        await CreateTableAsync(token);

        _job = new WordCloudJob(_service, _operation, _connection, _option);
        _scheduler = await StdSchedulerFactory.GetDefaultScheduler(token);
        _scheduler.JobFactory = new WordCloudJobFactory(_job);

        var job = JobBuilder.Create<WordCloudJob>()
            .WithIdentity("WordCloudJob", "WordCloud")
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity("WordCloudTrigger", "WordCloud")
            .WithCronSchedule(_option.Cron)
            .Build();

        await _scheduler.ScheduleJob(job, trigger, token);
        await _scheduler.Start(token);

        LogWordCloudJobScheduled(_logger);
    }

    public override async Task StopAsync(CancellationToken token)
    {
        await (_scheduler?.Shutdown(token) ?? Task.CompletedTask);
        await _connection.CloseAsync();
    }

    public Task OnCommandAsync(long selfId, MessageEvent @event, CancellationToken token) =>
        @event is GroupMessageEvent e ? _job!.SendWordCloudAsync(e.GroupId, token: token) : Task.CompletedTask;

    #region Log

    [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Word cloud job scheduled")]
    private static partial void LogWordCloudJobScheduled(ILogger logger);

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Option binding failed")]
    private static partial void LogOptionBindingFailed(ILogger logger);

    #endregion
}