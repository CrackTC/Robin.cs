using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Robin.Abstractions;
using Robin.Abstractions.Event.Message;
using Robin.Abstractions.Message.Entity;
using Robin.Abstractions.Operation.Requests;
using Robin.Extensions.Gemini.Entity;
using Robin.Extensions.Gemini.Entity.Responses;
using System.Text.Json;
using System.Text.RegularExpressions;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Context;
using Robin.Fluent;
using Robin.Fluent.Event;

namespace Robin.Extensions.Gemini;

[BotFunctionInfo("gemini", "Gemini 聊天机器人")]
// ReSharper disable once UnusedType.Global
public partial class GeminiFunction(FunctionContext context) : BotFunction(context), IFluentFunction
{
    private GeminiOption? _option;

    private Regex? _modelRegex;
    private Regex? _systemRegex;
    private Regex? _clearRegex;
    private Regex? _rollbackRegex;

    public string? Description { get; set; }

    public async Task OnCreatingAsync(FunctionBuilder builder, CancellationToken token)
    {
        if (_context.Configuration.Get<GeminiOption>() is not { } option)
        {
            LogOptionBindingFailed(_context.Logger);
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
            LogRegexCompileFailed(_context.Logger, e);
            return;
        }

        await CreateTablesAsync(token);

        builder
            .On<PrivateMessageEvent>()
            .OnRegex(_clearRegex)
            .Do(async tuple =>
            {
                var (ctx, _) = tuple;
                var (e, t) = ctx;
                await RemoveAllAsync(e.UserId, t);
                await SendReplyAsync(e.UserId, _option.ClearReply, t);
            })

            .On<PrivateMessageEvent>()
            .OnRegex(_rollbackRegex)
            .Do(async tuple =>
            {
                var (ctx, _) = tuple;
                var (e, t) = ctx;
                await RemoveLastAsync(e.UserId, t);
                await SendReplyAsync(e.UserId, _option.RollbackReply, t);
            })

            .On<PrivateMessageEvent>()
            .OnRegex(_modelRegex)
            .Do(async tuple =>
            {
                var (ctx, match) = tuple;
                var (e, t) = ctx;
                await SetModelAsync(e.UserId, match.Groups[1].Value, t);
                await SendReplyAsync(e.UserId, _option.ModelReply, t);
            })
            .On<PrivateMessageEvent>()
            .OnRegex(_systemRegex)
            .Do(async tuple =>
            {
                var (ctx, match) = tuple;
                var (e, t) = ctx;
                await SetSystemAsync(e.UserId, match.Groups[1].Value, t);
                await SendReplyAsync(e.UserId, _option.SystemReply, t);
            })
            .On<PrivateMessageEvent>()
            .OnText()
            .AsFallback()
            .Do(async tuple =>
            {
                var (ctx, text) = tuple;
                var (e, t) = ctx;

                var model = await GetModelAsync(e.UserId, t);
                var system = await GetSystem(e.UserId, t);

                List<GeminiContent> contents =
                [
                    .. await GetHistoryAsync(e.UserId, t),
                    new GeminiContent
                    {
                        Parts = [new GeminiPart { Text = text }],
                        Role = GeminiRole.User
                    }
                ];

                var now = DateTimeOffset.Now.ToUnixTimeSeconds();
                var request = new GeminiRequest(_option.ApiKey, model: model);
                if (await request.GenerateContentAsync(
                        new GeminiRequestBody
                        {
                            Contents = contents,
                            SystemInstruction = string.IsNullOrEmpty(system)
                                    ? null
                                    : new GeminiContent { Parts = [new GeminiPart { Text = system }] }
                        },
                        t
                    ) is not { } response)
                {
                    LogGenerateContentFailed(_context.Logger, e.UserId);
                    await SendReplyAsync(e.UserId, _option.ErrorReply, t);
                    return;
                }

                if (response is GeminiErrorResponse errorResponse)
                {
                    LogGenerateContentFailed(_context.Logger, e.UserId);
                    await SendReplyAsync(e.UserId, JsonSerializer.Serialize(errorResponse), t);
                    return;
                }

                if (response is not GeminiGenerateDataResponse { Candidates: { Count: > 0 } candidates })
                {
                    LogGenerateContentFailed(_context.Logger, e.UserId);
                    await SendReplyAsync(e.UserId, _option.FilteredReply, t);
                    return;
                }

                if (candidates[0].Content.Parts[0] is { Text: { } reply })
                {
                    if (!await SendReplyAsync(e.UserId, reply, t)) return;
                    await AddHistoryAsync(e.UserId, GeminiRole.User, text, now, t);
                    await AddHistoryAsync(e.UserId, GeminiRole.Model, reply, now, t);
                }
                else
                {
                    await SendReplyAsync(e.UserId, "Invalid response", t);
                }
            });
    }

    private async Task<bool> SendReplyAsync(long userId, string reply, CancellationToken token) =>
        await new SendPrivateMessageRequest(userId, [
            new TextData(reply)
        ]).SendAsync(_context.OperationProvider, _context.Logger, token) is not null;

    public override async Task StopAsync(CancellationToken token)
    {
        _semaphore.Dispose();
        await _db.DisposeAsync();
    }

    #region Database

    private readonly GeminiDbContext _db = new(context.Uin);
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
                    Parts = new List<GeminiPart> { new() { Text = msg.Content } },
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

    #endregion

    #region Log

    [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Option binding failed")]
    private static partial void LogOptionBindingFailed(ILogger logger);

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Regex compile failed")]
    private static partial void LogRegexCompileFailed(ILogger logger, ArgumentException exception);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Failed to generate content for user {UserId}")]
    private static partial void LogGenerateContentFailed(ILogger logger, long userId);

    #endregion
}
