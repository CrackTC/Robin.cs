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
public partial class GeminiFunction(
    IServiceProvider service,
    long uin,
    IOperationProvider operation,
    IConfiguration configuration,
    IEnumerable<BotFunction> functions
) : BotFunction(service, uin, operation, configuration, functions), IFilterHandler
{
    private readonly ILogger<GeminiFunction> _logger = service.GetRequiredService<ILogger<GeminiFunction>>();
    private GeminiOption? _option;

    private Regex? _modelRegex;
    private Regex? _systemRegex;
    private Regex? _clearRegex;
    private Regex? _rollbackRegex;

    private readonly GeminiDbContext _db = new(uin);
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private Task<bool> CreateTablesAsync(CancellationToken token) => _db.Database.EnsureCreatedAsync(token);

    private async Task RemoveLastAsync(long userId, CancellationToken token)
    {
        await _semaphore.WaitAsync(token);
        try
        {
            var last = _db.Messages
                .Where(msg => msg.User.UserId == userId)
                .OrderByDescending(msg => msg.Timestamp)
                .Take(2);

            _db.Messages.RemoveRange(last);
            await _db.SaveChangesAsync(token);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task RemoveAllAsync(long userId, CancellationToken token)
    {
        await _semaphore.WaitAsync(token);
        try
        {
            var all = _db.Messages
                .Where(msg => msg.User.UserId == userId);

            _db.Messages.RemoveRange(all);
            await _db.SaveChangesAsync(token);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<IEnumerable<GeminiContent>> GetHistoryAsync(long userId, CancellationToken token)
    {
        await _semaphore.WaitAsync(token);
        try
        {
            return _db.Messages
                .Where(msg => msg.User.UserId == userId)
                .Select(msg => new GeminiContent
                {
                    Parts = new List<GeminiTextPart>
                    {
                        new()
                        {
                            Text = msg.Content
                        }
                    },
                    Role = msg.Role
                });
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task AddHistoryAsync(long userId, GeminiRole role, string content, long timestamp,
        CancellationToken token)
    {
        await _semaphore.WaitAsync(token);
        try
        {
            var user = await _db.Users.FindAsync([userId], token);
            if (user is null)
            {
                user = new User
                {
                    UserId = userId,
                    ModelName = _option!.Model,
                    SystemCommand = string.Empty
                };
                _db.Users.Add(user);
            }

            user.Messages.Add(new Message
            {
                User = user,
                Role = role,
                Content = content,
                Timestamp = timestamp
            });

            await _db.SaveChangesAsync(token);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<string> GetModelAsync(long userId, CancellationToken token)
    {
        await _semaphore.WaitAsync(token);
        try
        {
            var user = await _db.Users.FindAsync([userId], token);
            return user?.ModelName ?? _option!.Model;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task SetModelAsync(long userId, string model, CancellationToken token)
    {
        await _semaphore.WaitAsync(token);
        try
        {
            var user = await _db.Users.FindAsync([userId], token);
            if (user is null)
            {
                user = new User
                {
                    UserId = userId,
                    ModelName = model,
                    SystemCommand = string.Empty
                };
                _db.Users.Add(user);
            }
            else
            {
                user.ModelName = model;
            }

            await _db.SaveChangesAsync(token);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<string> GetSystem(long userId, CancellationToken token)
    {
        await _semaphore.WaitAsync(token);
        try
        {
            var user = await _db.Users.FindAsync([userId], token);
            return user?.SystemCommand ?? string.Empty;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task SetSystemAsync(long userId, string system, CancellationToken token)
    {
        await _semaphore.WaitAsync(token);
        try
        {
            var user = await _db.Users.FindAsync([userId], token);
            if (user is null)
            {
                user = new User
                {
                    UserId = userId,
                    ModelName = _option!.Model,
                    SystemCommand = system
                };
                _db.Users.Add(user);
            }
            else
            {
                user.SystemCommand = system;
            }

            await _db.SaveChangesAsync(token);
        }
        finally
        {
            _semaphore.Release();
        }
    }

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
        await AddHistoryAsync(e.UserId, GeminiRole.User, text, now, token);
        await AddHistoryAsync(e.UserId, GeminiRole.Model, content, now, token);

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

        await CreateTablesAsync(token);
    }

    public override async Task StopAsync(CancellationToken token)
    {
        _semaphore.Dispose();
        await _db.DisposeAsync();
    }

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