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
using Robin.Middlewares.Fluent;
using Robin.Middlewares.Fluent.Event;
using Robin.Abstractions.Utility;
using Microsoft.EntityFrameworkCore;

namespace Robin.Extensions.Gemini;

[BotFunctionInfo("gemini", "Gemini 聊天机器人")]
public partial class GeminiFunction(
    FunctionContext<GeminiOption> context
) : BotFunction<GeminiOption>(context), IFluentFunction
{
    private Regex? _modelRegex;
    private Regex? _systemRegex;
    private Regex? _clearRegex;
    private Regex? _rollbackRegex;

    public async Task OnCreatingAsync(FunctionBuilder builder, CancellationToken token)
    {
        try
        {
            _modelRegex = new Regex(_context.Configuration.ModelRegexString, RegexOptions.Compiled);
            _systemRegex = new Regex(_context.Configuration.SystemRegexString, RegexOptions.Compiled);
            _clearRegex = new Regex(_context.Configuration.ClearRegexString, RegexOptions.Compiled);
            _rollbackRegex = new Regex(_context.Configuration.RollbackRegexString, RegexOptions.Compiled);
        }
        catch (ArgumentException e)
        {
            LogRegexCompileFailed(_context.Logger, e);
            return;
        }

        await CreateTablesAsync(token);

        builder
            .On<PrivateMessageEvent>("clear history")
            .OnRegex(_clearRegex)
            .Do(async tuple =>
            {
                var (ctx, _) = tuple;
                var (e, t) = ctx;
                await RemoveAllAsync(e.UserId, t);
                await SendReplyAsync(e.UserId, _context.Configuration.ClearReply, t);
            })
            .On<PrivateMessageEvent>("rollback history")
            .OnRegex(_rollbackRegex)
            .Do(async tuple =>
            {
                var (ctx, _) = tuple;
                var (e, t) = ctx;
                await RemoveLastAsync(e.UserId, t);
                await SendReplyAsync(e.UserId, _context.Configuration.RollbackReply, t);
            })
            .On<PrivateMessageEvent>("switch model")
            .OnRegex(_modelRegex)
            .Do(async tuple =>
            {
                var (ctx, match) = tuple;
                var (e, t) = ctx;
                await SetModelAsync(e.UserId, match.Groups[1].Value, t);
                await SendReplyAsync(e.UserId, _context.Configuration.ModelReply, t);
            })
            .On<PrivateMessageEvent>("switch system message")
            .OnRegex(_systemRegex)
            .Do(async tuple =>
            {
                var (ctx, match) = tuple;
                var (e, t) = ctx;
                await SetSystemAsync(e.UserId, match.Groups[1].Value, t);
                await SendReplyAsync(e.UserId, _context.Configuration.SystemReply, t);
            })
            .On<PrivateMessageEvent>("chat")
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
                var request = new GeminiRequest(_context.Configuration.ApiKey, model: model);
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
                    await SendReplyAsync(e.UserId, _context.Configuration.ErrorReply, t);
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
                    await SendReplyAsync(e.UserId, _context.Configuration.FilteredReply, t);
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
        ]).SendAsync(_context, token) is not null;

    public override async Task StopAsync(CancellationToken token)
    {
        _semaphore.Dispose();
        await _db.DisposeAsync();
    }

    #region Database

    private readonly GeminiDbContext _db = new(context.BotContext.Uin);
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private Task<bool> CreateTablesAsync(CancellationToken token) =>
        _semaphore.ConsumeAsync(() => _db.Database.EnsureCreatedAsync(token), token);

    private Task<int> RemoveLastAsync(long userId, CancellationToken token) =>
        _semaphore.ConsumeAsync(() =>
        {
            var last = _db.Messages
                .Where(msg => msg.User.UserId == userId)
                .OrderByDescending(msg => msg.Timestamp)
                .Take(2);

            _db.Messages.RemoveRange(last);
            return _db.SaveChangesAsync(token);
        }, token);

    private Task<int> RemoveAllAsync(long userId, CancellationToken token) =>
        _semaphore.ConsumeAsync(() =>
        {
            var all = _db.Messages.Where(msg => msg.User.UserId == userId);

            _db.Messages.RemoveRange(all);
            return _db.SaveChangesAsync(token);
        }, token);

    private Task<List<GeminiContent>> GetHistoryAsync(long userId, CancellationToken token) =>
        _semaphore.ConsumeAsync(() => _db.Messages
            .Where(msg => msg.User.UserId == userId)
            .OrderBy(msg => msg.Timestamp)
            .Select(msg => new GeminiContent
            {
                Parts = new List<GeminiPart> { new() { Text = msg.Content } },
                Role = msg.Role
            })
            .ToListAsync(token), token);

    private Task<int> AddHistoryAsync(
        long userId,
        GeminiRole role,
        string content,
        long timestamp,
        CancellationToken token
    ) =>
        _semaphore.ConsumeAsync(async Task<int> () =>
        {
            var user = await _db.Users.FindAsync([userId], token);
            if (user is null)
            {
                user = new User
                {
                    UserId = userId,
                    ModelName = _context.Configuration.Model,
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

            return await _db.SaveChangesAsync(token);
        }, token);

    private ValueTask<string> GetModelAsync(long userId, CancellationToken token) =>
        _semaphore.ConsumeAsync(async ValueTask<string> () =>
        {
            var user = await _db.Users.FindAsync([userId], token);
            return user?.ModelName ?? _context.Configuration.Model;
        }, token);

    private Task<int> SetModelAsync(long userId, string model, CancellationToken token) =>
        _semaphore.ConsumeAsync(async Task<int> () =>
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

            return await _db.SaveChangesAsync(token);
        }, token);

    private ValueTask<string> GetSystem(long userId, CancellationToken token) =>
        _semaphore.ConsumeAsync(async ValueTask<string> () =>
        {
            var user = await _db.Users.FindAsync([userId], token);
            return user?.SystemCommand ?? string.Empty;
        }, token);

    private Task<int> SetSystemAsync(long userId, string system, CancellationToken token) =>
        _semaphore.ConsumeAsync(async Task<int> () =>
        {
            var user = await _db.Users.FindAsync([userId], token);
            if (user is null)
            {
                user = new User
                {
                    UserId = userId,
                    ModelName = _context.Configuration.Model,
                    SystemCommand = system
                };
                _db.Users.Add(user);
            }
            else
            {
                user.SystemCommand = system;
            }

            return await _db.SaveChangesAsync(token);
        }, token);

    #endregion

    #region Log

    [LoggerMessage(Level = LogLevel.Warning, Message = "Regex compile failed")]
    private static partial void LogRegexCompileFailed(ILogger logger, ArgumentException exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to generate content for user {UserId}")]
    private static partial void LogGenerateContentFailed(ILogger logger, long userId);

    #endregion
}
