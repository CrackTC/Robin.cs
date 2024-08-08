﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Robin.Abstractions;
using Robin.Abstractions.Context;
using Robin.Abstractions.Event.Message;
using Robin.Abstractions.Message;
using Robin.Abstractions.Message.Entity;
using Robin.Abstractions.Operation;
using Robin.Fluent;
using Robin.Fluent.Builder;

namespace Robin.Extensions.RandReply;

[BotFunctionInfo("rand_reply", "随机回复")]
// ReSharper disable once UnusedType.Global
public partial class RandReplyFunction(FunctionContext context) : BotFunction(context), IFluentFunction
{
    private RandReplyOption? _option;

    public string? Description { get; set; }

    public Task OnCreatingAsync(FunctionBuilder builder, CancellationToken _)
    {
        if (_context.Configuration.Get<RandReplyOption>() is not { } option)
        {
            LogOptionBindingFailed(_context.Logger);
            return Task.CompletedTask;
        }

        _option = option;

        builder.On<GroupMessageEvent>()
            .OnAtSelf(_context.Uin)
            .AsFallback()
            .Do(async ctx =>
            {

                var textCount = _option.Texts?.Count ?? 0;
                var imageCount = _option.ImagePaths?.Count ?? 0;
                var index = Random.Shared.Next(textCount + imageCount);

                SegmentData content = index < textCount
                    ? new TextData(_option.Texts![index])
                    : new ImageData($"base64://{Convert.ToBase64String(await File.ReadAllBytesAsync(
                        _option.ImagePaths![index - textCount],
                        ctx.Token
                    ))}");

                if (await ctx.Event.NewMessageRequest([new ReplyData(ctx.Event.MessageId), content])
                        .SendAsync(_context.OperationProvider, ctx.Token) is not { Success: true })
                {
                    LogSendFailed(_context.Logger, ctx.Event.GroupId);
                    return;
                }

                LogReplySent(_context.Logger, ctx.Event.GroupId);
            });

        return Task.CompletedTask;
    }

    #region Log

    [LoggerMessage(EventId = 0, Level = LogLevel.Error, Message = "Failed to bind option.")]
    private static partial void LogOptionBindingFailed(ILogger logger);

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Send message failed for group {GroupId}")]
    private static partial void LogSendFailed(ILogger logger, long groupId);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Reply sent for group {GroupId}")]
    private static partial void LogReplySent(ILogger logger, long groupId);

    #endregion
}
