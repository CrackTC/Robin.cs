using Robin.Abstractions;
using Robin.Abstractions.Context;
using Robin.Abstractions.Event.Message;
using Robin.Abstractions.Message;
using Robin.Abstractions.Message.Entity;
using Robin.Middlewares.Fluent;
using Sorac.OpSsd1306;

namespace Robin.Extensions.Oled;

[BotFunctionInfo("oled", "OLED display")]
public class OledFunction(FunctionContext context) : BotFunction(context), IFluentFunction
{
    private static readonly OpSsd1306 _oled = new OpSsd1306(0, 1, 12);
    private static readonly Font _font = new Font("wenquanyi_9pt.pcf");

    static OledFunction() => _oled.Clear();

    private static string GetText(MessageChain chain) => string.Join(' ', chain.Select(msg => msg switch
    {
        TextData { Text: var text } => text,
        AtData { Uin: var uin } => $"[@{uin}]",
        ReplyData { Id: var id } => $"[re:{id}]",
        ImageData => "[图片]",
        _ => $"[{msg.GetType().Name}]"
    }));

    public Task OnCreatingAsync(FunctionBuilder builder, CancellationToken token)
    {
        builder.On<MessageEvent>()
            .AsAlwaysFired()
            .Do((tuple) =>
            {
                var (e, t) = tuple;
                _oled.Print(_font, $"\n{e.UserId}: {GetText(e.Message)}");
                _oled.SyncLine();
                return Task.CompletedTask;
            });

        return Task.CompletedTask;
    }
}
