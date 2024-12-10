using Robin.Abstractions;
using Robin.Abstractions.Context;
using Robin.Abstractions.Event.Message;
using Robin.Abstractions.Message.Entity;
using Robin.Abstractions.Operation.Requests;
using Robin.Middlewares.Fluent;
using Sorac.OpSsd1306;
using Robin.Abstractions.Operation;
using System.Text;
using Robin.Abstractions.Utility;

namespace Robin.Extensions.Oled;

[BotFunctionInfo("oled", "OLED display")]
public class OledFunction(FunctionContext context) : BotFunction(context), IFluentFunction
{
    private static readonly OpSsd1306 _oled = new(sdaPort: 0, sclPort: 1, lineHeight: 12);
    private static readonly Font _font = new("wenquanyi_9pt.pcf");
    private static readonly SemaphoreSlim _semaphore = new(1, 1);

    static OledFunction()
    {
        _oled.Clear();
        _oled.Print(_font, "=w=");
        _oled.SyncLine();
    }

    private async Task<string> GetSourceName(MessageEvent e, CancellationToken token) => e switch
    {
        GroupMessageEvent { SourceId: var groupId } when
            await new GetGroupInfoRequest(groupId)
                .SendAsync(_context, token) is { Info: { } info } =>
                    info.GroupName,
        PrivateMessageEvent { Sender.Nickname: var nickname } => nickname,
        _ => e.SourceId.ToString()
    };

    private async Task<string> GetUserName(MessageEvent e, long uin, CancellationToken token) => e switch
    {
        GroupMessageEvent { Sender: { } sender } when sender.UserId == uin =>
            sender.Card switch { null or "" => sender.Nickname, _ => sender.Card },
        GroupMessageEvent { GroupId: var groupId } when
            await new GetGroupMemberInfoRequest(groupId, uin).SendAsync(_context, token) is { Info: { } info } =>
                    info.Card switch { null or "" => info.Nickname, _ => info.Card },
        PrivateMessageEvent { Sender.Nickname: var nickname } => nickname,
        _ => uin.ToString()
    };

    private async Task<string> GetText(MessageEvent e, CancellationToken token)
    {
        if (e.Message.FirstOrDefault() is JsonData) return "[JSON]";

        return string.Join(' ', (await Task.WhenAll(e.Message.Select(async msg => msg switch
        {
            AtData { Uin: var uin } => $"@{await GetUserName(e, uin, token)}",
            FaceData => "[表情]",
            ForwardData => "[转发]",
            ImageData { Summary: var summary } => summary ?? "[图片]",
            MarketFaceData => string.Empty, // TextData will handle this
            ReplyData => $"[回复]",
            TextData { Text: var text } => text.Trim(),
            VideoData => "[视频]",
            _ => $"[{msg.GetType().Name}]"
        }))).Where(s => !string.IsNullOrWhiteSpace(s)));
    }

    public Task OnCreatingAsync(FunctionBuilder builder, CancellationToken token)
    {
        var (lastSource, lastUser) = ((typeof(MessageEvent), 0L), 0L);
        var textBuilder = new StringBuilder();
        builder.On<MessageEvent>()
            .AsIntrinsic()
            .Do((tuple) => _semaphore.ConsumeAsync(async Task () =>
            {
                var (e, t) = tuple;
                var (newSource, newUser) = ((e.GetType(), e.SourceId), e.UserId);

                if (newSource != lastSource)
                    textBuilder.AppendLine().Append('*').Append(await GetSourceName(e, t));
                if ((newSource, newUser) != (lastSource, lastUser))
                    textBuilder.AppendLine().Append('>').Append(await GetUserName(e, e.UserId, t));
                textBuilder.AppendLine().Append('|').Append(await GetText(e, t));

                _oled.Print(_font, textBuilder.ToString());
                _oled.SyncLine();
                textBuilder.Clear();

                (lastSource, lastUser) = (newSource, newUser);
            }, tuple.Token));

        return Task.CompletedTask;
    }
}
