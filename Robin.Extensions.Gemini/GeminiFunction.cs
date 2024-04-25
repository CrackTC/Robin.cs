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
using Robin.Extensions.Gemini.Entities;
using Robin.Extensions.Gemini.Entities.Responses;
using System.Text.Json;
using System.Text.RegularExpressions;
using Robin.Annotations.Filters;
using Robin.Annotations.Filters.Message;

namespace Robin.Extensions.Gemini;

[BotFunctionInfo("gemini", "Gemini chat bot")]
[OnPrivateMessage, Fallback]
// ReSharper disable once UnusedType.Global
public partial class GeminiFunction : BotFunction, IFilterHandler
{
    private readonly SqliteConnection _connection;
    private readonly ILogger<GeminiFunction> _logger;
    private GeminiOption? _option;

    private Regex? _modelRegex;
    private Regex? _systemRegex;
    private Regex? _clearRegex;
    private Regex? _rollbackRegex;

    #region Commands

    private readonly SqliteCommand _createMessageTableCommand;

    private const string CreateMessageTableSql =
        "CREATE TABLE IF NOT EXISTS gemini (user_id INTEGER NOT NULL, role TEXT NOT NULL, content TEXT NOT NULL, timestamp INTEGER NOT NULL)";

    private readonly SqliteCommand _createModelTableCommand;

    private const string CreateModelTableSql =
        "CREATE TABLE IF NOT EXISTS gemini_model (user_id INTEGER NOT NULL PRIMARY KEY, model TEXT NOT NULL)";

    private readonly SqliteCommand _createSystemTableCommand;

    private const string CreateSystemTableSql =
        "CREATE TABLE IF NOT EXISTS gemini_system (user_id INTEGER NOT NULL PRIMARY KEY, system TEXT NOT NULL)";

    private readonly SqliteCommand _removeLastCommand;

    private const string RemoveLastCommandSql =
        "DELETE FROM gemini WHERE timestamp IN (SELECT timestamp FROM gemini WHERE user_id = $userId ORDER BY timestamp DESC LIMIT 2) AND user_id = $userId";

    private readonly SqliteCommand _removeAllCommand;

    private const string RemoveAllCommandSql =
        "DELETE FROM gemini WHERE user_id = $userId";

    private readonly SqliteCommand _getHistoryCommand;

    private const string GetHistoryCommandSql =
        "SELECT role, content FROM gemini WHERE user_id = $userId ORDER BY timestamp";

    private readonly SqliteCommand _addHistoryCommand;

    private const string AddHistoryCommandSql =
        "INSERT INTO gemini (user_id, role, content, timestamp) VALUES ($userId, $role, $content, $timestamp)";

    private readonly SqliteCommand _getModelCommand;

    private const string GetModelCommandSql =
        "SELECT model FROM gemini_model WHERE user_id = $userId";

    private readonly SqliteCommand _setModelCommand;

    private const string SetModelCommandSql =
        "INSERT INTO gemini_model (user_id, model) VALUES ($userId, $model) ON CONFLICT(user_id) DO UPDATE SET model = $model";

    private readonly SqliteCommand _getSystemCommand;

    private const string GetSystemCommand =
        "SELECT system FROM gemini_system WHERE user_id = $userId";

    private readonly SqliteCommand _setSystemCommand;

    #endregion

    private async Task CreateTablesAsync(CancellationToken token)
    {
        await _createMessageTableCommand.ExecuteNonQueryAsync(token);
        await _createModelTableCommand.ExecuteNonQueryAsync(token);
        await _createSystemTableCommand.ExecuteNonQueryAsync(token);
    }

    private async Task RemoveLastAsync(long userId, CancellationToken token)
    {
        _removeLastCommand.Parameters.AddWithValue("$userId", userId);
        await _removeLastCommand.ExecuteNonQueryAsync(token);
        _removeLastCommand.Parameters.Clear();
    }

    private async Task RemoveAllAsync(long userId, CancellationToken token)
    {
        _removeAllCommand.Parameters.AddWithValue("$userId", userId);
        await _removeAllCommand.ExecuteNonQueryAsync(token);
        _removeAllCommand.Parameters.Clear();
    }

    private async Task<IEnumerable<GeminiContent>> GetHistoryAsync(long userId, CancellationToken token)
    {
        _getHistoryCommand.Parameters.AddWithValue("$userId", userId);
        await using var reader = await _getHistoryCommand.ExecuteReaderAsync(token);
        var history = new List<GeminiContent>();
        while (await reader.ReadAsync(token))
        {
            var role = reader.GetString(0);
            var content = reader.GetString(1);
            history.Add(new GeminiContent
            {
                Parts =
                [
                    new GeminiTextPart
                    {
                        Text = content
                    }
                ],
                Role = role is "user" ? GeminiRole.User : GeminiRole.Model
            });
        }

        _getHistoryCommand.Parameters.Clear();
        return history;
    }

    private async Task AddHistoryAsync(long userId, string role, string content, long timestamp,
        CancellationToken token)
    {
        _addHistoryCommand.Parameters.AddWithValue("$userId", userId);
        _addHistoryCommand.Parameters.AddWithValue("$role", role);
        _addHistoryCommand.Parameters.AddWithValue("$content", content);
        _addHistoryCommand.Parameters.AddWithValue("$timestamp", timestamp);
        await _addHistoryCommand.ExecuteNonQueryAsync(token);
        _addHistoryCommand.Parameters.Clear();
    }

    private async Task<string> GetModelAsync(long userId, CancellationToken token)
    {
        _getModelCommand.Parameters.AddWithValue("$userId", userId);
        var model = await _getModelCommand.ExecuteScalarAsync(token);
        _getModelCommand.Parameters.Clear();
        return model as string ?? _option!.Model;
    }

    private async Task SetModelAsync(long userId, string model, CancellationToken token)
    {
        _setModelCommand.Parameters.AddWithValue("$userId", userId);
        _setModelCommand.Parameters.AddWithValue("$model", model);
        await _setModelCommand.ExecuteNonQueryAsync(token);
        _setModelCommand.Parameters.Clear();
    }

    private async Task<string> GetSystem(long userId, CancellationToken token)
    {
        _getSystemCommand.Parameters.AddWithValue("$userId", userId);
        var system = await _getSystemCommand.ExecuteScalarAsync(token);
        _getSystemCommand.Parameters.Clear();
        return system as string ?? string.Empty;
    }

    private async Task SetSystemAsync(long userId, string system, CancellationToken token)
    {
        _setSystemCommand.Parameters.AddWithValue("$userId", userId);
        _setSystemCommand.Parameters.AddWithValue("$system", system);
        await _setSystemCommand.ExecuteNonQueryAsync(token);
        _setSystemCommand.Parameters.Clear();
    }

    public GeminiFunction(IServiceProvider service,
        long uin,
        IOperationProvider operation,
        IConfiguration configuration,
        IEnumerable<BotFunction> functions) : base(service, uin, operation, configuration, functions)
    {
        _connection = new SqliteConnection($"Data Source=gemini-{uin}.db");

        _logger = service.GetRequiredService<ILogger<GeminiFunction>>();

        _createMessageTableCommand = _connection.CreateCommand();
        _createMessageTableCommand.CommandText = CreateMessageTableSql;

        _createModelTableCommand = _connection.CreateCommand();
        _createModelTableCommand.CommandText = CreateModelTableSql;

        _createSystemTableCommand = _connection.CreateCommand();
        _createSystemTableCommand.CommandText = CreateSystemTableSql;

        _removeLastCommand = _connection.CreateCommand();
        _removeLastCommand.CommandText = RemoveLastCommandSql;

        _removeAllCommand = _connection.CreateCommand();
        _removeAllCommand.CommandText = RemoveAllCommandSql;

        _getHistoryCommand = _connection.CreateCommand();
        _getHistoryCommand.CommandText = GetHistoryCommandSql;

        _addHistoryCommand = _connection.CreateCommand();
        _addHistoryCommand.CommandText = AddHistoryCommandSql;

        _getModelCommand = _connection.CreateCommand();
        _getModelCommand.CommandText = GetModelCommandSql;

        _setModelCommand = _connection.CreateCommand();
        _setModelCommand.CommandText = SetModelCommandSql;

        _getSystemCommand = _connection.CreateCommand();
        _getSystemCommand.CommandText = GetSystemCommand;

        _setSystemCommand = _connection.CreateCommand();
        _setSystemCommand.CommandText = SetSystemCommandSql;
    }

    private const string SetSystemCommandSql =
        "INSERT INTO gemini_system (user_id, system) VALUES ($userId, $system) ON CONFLICT(user_id) DO UPDATE SET system = $system";

    private async Task SendReplyAsync(long userId, string reply, CancellationToken token)
    {
        if (await _operation.SendRequestAsync(
                new SendPrivateMessageRequest(userId, [new TextData(reply)]), token) is not
            { Success: true })
        {
            LogSendFailed(_logger, userId);
        }
    }


    public async Task<bool> OnFilteredEventAsync(int filterGroup, long selfId, BotEvent @event, CancellationToken token)
    {
        if (@event is not PrivateMessageEvent e) return false;

        var text = string.Join(' ', e.Message.OfType<TextData>().Select(s => s.Text)).Trim();
        if (text.Length == 0) return false;

        if (_clearRegex!.IsMatch(text))
        {
            await RemoveAllAsync(e.UserId, token);
            await SendReplyAsync(e.UserId, _option!.ClearReply, token);
            return true;
        }

        if (_rollbackRegex!.IsMatch(text))
        {
            await RemoveLastAsync(e.UserId, token);
            await SendReplyAsync(e.UserId, _option!.RollbackReply, token);
            return true;
        }

        if (_modelRegex!.Match(text) is { Success: true, Groups: { Count: 2 } modelGroups })
        {
            await SetModelAsync(e.UserId, modelGroups[1].Value, token);
            await SendReplyAsync(e.UserId, _option!.ModelReply, token);
            return true;
        }

        if (_systemRegex!.Match(text) is { Success: true, Groups: { Count: 2 } systemGroups })
        {
            await SetSystemAsync(e.UserId, systemGroups[1].Value, token);
            await SendReplyAsync(e.UserId, _option!.SystemReply, token);
            return true;
        }

        var model = await GetModelAsync(e.UserId, token);
        var system = await GetSystem(e.UserId, token);

        var contents = new List<GeminiContent>();
        if (!string.IsNullOrEmpty(system))
            contents.Add(new GeminiContent
            {
                Parts =
                [
                    new GeminiTextPart
                    {
                        Text = system
                    }
                ],
                Role = GeminiRole.User
            });

        contents.AddRange([
            .. await GetHistoryAsync(e.UserId, token),
            new GeminiContent
            {
                Parts =
                [
                    new GeminiTextPart
                    {
                        Text = text
                    }
                ],
                Role = GeminiRole.User
            }
        ]);

        var now = DateTimeOffset.Now.ToUnixTimeSeconds();
        var request = new GeminiRequest(_option!.ApiKey, model: model);
        if (await request.GenerateContentAsync(new GeminiRequestBody
            {
                Contents = contents
            }, token) is not
            { } response)
        {
            LogGenerateContentFailed(_logger, e.UserId);
            await SendReplyAsync(e.UserId, _option!.ErrorReply, token);
            return true;
        }

        if (response is GeminiErrorResponse errorResponse)
        {
            LogGenerateContentFailed(_logger, e.UserId);
            await SendReplyAsync(e.UserId, JsonSerializer.Serialize(errorResponse), token);
            return true;
        }

        if (response is not GeminiGenerateDataResponse { Candidates.Count: > 0 } r)
        {
            LogGenerateContentFailed(_logger, e.UserId);
            await SendReplyAsync(e.UserId, _option!.FilteredReply, token);
            return true;
        }

        var content = r.Candidates[0].Content.Parts[0].Text;
        await AddHistoryAsync(e.UserId, "user", text, now, token);
        await AddHistoryAsync(e.UserId, "model", content, now, token);

        var textData = new TextData(content);

        if (await _operation.SendRequestAsync(new SendPrivateMessageRequest(e.UserId, [textData]), token) is not { Success: true })
        {
            LogSendFailed(_logger, e.UserId);
            await SendReplyAsync(e.UserId, _option!.ErrorReply, token);
            return true;
        }

        LogReplySent(_logger, e.UserId);
        return true;
    }

    public override Task OnEventAsync(long selfId, BotEvent @event, CancellationToken token) => throw new InvalidOperationException();
    public override async Task StartAsync(CancellationToken token)
    {
        if (_configuration.Get<GeminiOption>() is not { } option)
        {
            LogOptionBindingFailed(_logger);
            return;
        }

        _option = option;

        try
        {
            _modelRegex = new Regex(option.ModelRegexString, RegexOptions.Compiled);
            _systemRegex = new Regex(option.SystemRegexString, RegexOptions.Compiled);
            _clearRegex = new Regex(option.ClearRegexString, RegexOptions.Compiled);
            _rollbackRegex = new Regex(option.RollbackRegexString, RegexOptions.Compiled);
        }
        catch (ArgumentException e)
        {
            LogRegexCompileFailed(_logger, e);
            return;
        }

        await _connection.OpenAsync(token);
        await CreateTablesAsync(token);
    }

    public override Task StopAsync(CancellationToken token) => _connection.CloseAsync();

    #region Log

    [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Option binding failed")]
    private static partial void LogOptionBindingFailed(ILogger logger);

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Regex compile failed")]
    private static partial void LogRegexCompileFailed(ILogger logger, ArgumentException exception);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Send message failed for user {UserId}")]
    private static partial void LogSendFailed(ILogger logger, long userId);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "Failed to generate content for user {UserId}")]
    private static partial void LogGenerateContentFailed(ILogger logger, long userId);

    [LoggerMessage(EventId = 5, Level = LogLevel.Information, Message = "Reply sent for group {UserId}")]
    private static partial void LogReplySent(ILogger logger, long userId);

    #endregion
}