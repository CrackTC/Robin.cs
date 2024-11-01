using Robin.Abstractions;
using Robin.Abstractions.Context;
using Robin.Abstractions.Event.Message;
using Robin.Abstractions.Message;
using Robin.Abstractions.Message.Entity;
using Robin.Abstractions.Operation;
using Robin.Middlewares.Fluent;
using Robin.Middlewares.Fluent.Event;

namespace Robin.Extensions.RandReply;

[BotFunctionInfo("rand_reply", "随机回复")]
public partial class RandReplyFunction(
    FunctionContext<RandReplyOption> context
) : BotFunction<RandReplyOption>(context), IFluentFunction
{
    public Task OnCreatingAsync(FunctionBuilder builder, CancellationToken _)
    {
        builder.On<GroupMessageEvent>()
            .OnAtSelf(_context.BotContext.Uin)
            .AsFallback()
            .Do(async ctx =>
            {

                var textCount = _context.Configuration.Texts?.Count ?? 0;
                var imageCount = _context.Configuration.ImagePaths?.Count ?? 0;
                var index = Random.Shared.Next(textCount + imageCount);

                SegmentData content = index < textCount
                    ? new TextData(_context.Configuration.Texts![index])
                    : new ImageData($"base64://{Convert.ToBase64String(await File.ReadAllBytesAsync(
                        _context.Configuration.ImagePaths![index - textCount],
                        ctx.Token
                    ))}");

                await ctx.Event.NewMessageRequest([new ReplyData(ctx.Event.MessageId), content])
                    .SendAsync(_context.BotContext.OperationProvider, _context.Logger, ctx.Token);
            });

        return Task.CompletedTask;
    }
}
