using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Robin.Abstractions;
using Robin.Abstractions.Context;
using Robin.Abstractions.Event.Message;
using Robin.Abstractions.Message.Entity;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Requests;
using Robin.Abstractions.Operation.Responses;
using Robin.Middlewares.Fluent;
using Robin.Middlewares.Fluent.Event;

namespace Robin.Extensions.Gray;

[BotFunctionInfo("gray", "喜多烧香精神续作（x")]
// ReSharper disable once UnusedType.Global
public partial class GrayFunction(FunctionContext context) : BotFunction(context), IFluentFunction
{
    private GrayOption? _option;

    public Task OnCreatingAsync(FunctionBuilder builder, CancellationToken token)
    {
        if (_context.Configuration.Get<GrayOption>() is not { } option)
        {
            LogOptionBindingFailed(_context.Logger);
            return Task.CompletedTask;
        }

        _option = option;

        builder.On<GroupMessageEvent>()
            .OnCommand("送走")
            .OnReply()
            .Do(async t =>
            {
                var (ctx, msgId) = t;

                if (await new GetMessageRequest(msgId)
                        .SendAsync<GetMessageResponse>(_context.OperationProvider, _context.Logger, ctx.Token)
                    is not { Message.Sender.UserId: var id }) return;

                var url = $"{_option.ApiAddress}/?id={id}";
                await ctx.Event.NewMessageRequest([
                    new ImageData(url)
                ]).SendAsync(_context.OperationProvider, _context.Logger, ctx.Token);
            });

        return Task.CompletedTask;
    }

    #region Log

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to bind option.")]
    private static partial void LogOptionBindingFailed(ILogger logger);

    #endregion
}
