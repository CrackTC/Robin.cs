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

namespace Robin.Extensions.WordCloud;

[BotFunctionInfo("word_cloud", "daily word cloud", typeof(GroupMessageEvent))]
public partial class WordCloudFunction : BotFunction
{
    private IScheduler? _scheduler;
    private WordCloudOption? _option;
    private readonly SqliteConnection _connection = new("Data Source=word_cloud.db");
    private readonly Logger<WordCloudFunction> _logger;
    private readonly SqliteCommand _createTableCommand;

    private const string CreateTableSql =
        "CREATE TABLE IF NOT EXISTS word_cloud (group_id INTEGER NOT NULL, message TEXT NOT NULL)";

    private readonly SqliteCommand _insertDataCommand;

    private const string InsertDataSql =
        "INSERT INTO word_cloud (group_id, message) VALUES ($group_id, $message)";

    public WordCloudFunction(
        IServiceProvider service,
        IOperationProvider operation,
        IConfiguration configuration) : base(service, operation, configuration)
    {
        _logger = service.GetRequiredService<Logger<WordCloudFunction>>();
        _createTableCommand = _connection.CreateCommand();
        _createTableCommand.CommandText = CreateTableSql;
        _insertDataCommand = _connection.CreateCommand();
        _insertDataCommand.CommandText = InsertDataSql;
    }

    private void CreateTable() => _createTableCommand.ExecuteNonQuery();

    private void InsertData(long groupId, string message)
    {
        _insertDataCommand.Parameters.AddWithValue("$group_id", groupId);
        _insertDataCommand.Parameters.AddWithValue("$message", message);
        _insertDataCommand.ExecuteNonQuery();
        _insertDataCommand.Parameters.Clear();
    }

    public override void OnEvent(long selfId, BotEvent @event)
    {
        if (@event is not GroupMessageEvent e) return;
        InsertData(e.GroupId,
            string.Join(' ', e.Message.Segments.Where(s => s is TextData).Select(s => (s as TextData)!.Text)));
    }

    public override async Task StartAsync(CancellationToken token)
    {
        if (_configuration.Get<WordCloudOption>() is not { } option)
        {
            LogOptionBindingFailed(_logger);
            return;
        }

        _option = option;

        CreateTable();

        _scheduler = await StdSchedulerFactory.GetDefaultScheduler(token);
        _scheduler.JobFactory = new WordCloudJobFactory(_service, _provider, _connection, _option);

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

    public override Task StopAsync(CancellationToken token)
        => _scheduler?.Shutdown(token) ?? Task.CompletedTask;

    #region Log

    [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Word cloud job scheduled")]
    private static partial void LogWordCloudJobScheduled(ILogger logger);

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Option binding failed")]
    private static partial void LogOptionBindingFailed(ILogger logger);

    #endregion
}