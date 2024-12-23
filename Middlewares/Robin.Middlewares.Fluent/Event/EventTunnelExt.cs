using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Robin.Abstractions.Context;
using Robin.Abstractions.Event.Message;
using Robin.Abstractions.Event.Notice;
using Robin.Abstractions.Message.Entity;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Requests;

namespace Robin.Middlewares.Fluent.Event;

public static class EventTunnelExt
{
    public static EventTunnelBuilder<EventContext<TEvent>> OnAt<TEvent>(
        this EventTunnelBuilder<EventContext<TEvent>> builder
    ) where TEvent : MessageEvent =>
        builder
            .Where(ctx => ctx.Event.Message.OfType<AtData>().Any())
            .WithDescription("消息@了某人");

    public static EventTunnelBuilder<EventContext<TEvent>> OnAt<TEvent>(
        this EventTunnelBuilder<EventContext<TEvent>> builder,
        long uin
    ) where TEvent : MessageEvent =>
        builder
            .Where(ctx => ctx.Event.Message.OfType<AtData>().Any(at => at.Uin == uin))
            .WithDescription($"消息@了 {uin}");

    public static EventTunnelBuilder<EventContext<TEvent>> OnNotAtOthers<TEvent>(
        this EventTunnelBuilder<EventContext<TEvent>> builder,
        long uin
    ) where TEvent : MessageEvent =>
        builder
            .Where(ctx => ctx.Event.Message.OfType<AtData>().All(at => at.Uin == uin))
            .WithDescription($"消息未@除了 {uin} 以外的人");

    public static EventTunnelBuilder<EventContext<TEvent>> OnCommand<TEvent>(
        this EventTunnelBuilder<EventContext<TEvent>> builder,
        string command,
        string prefix = "/"
    ) where TEvent : MessageEvent =>
        builder
            .Where(ctx => ctx.Event.Message.Any(seg => seg is TextData { Text: var text }
                && text.Trim().Split(null).Any(t => t == $"{prefix}{command}")))
            .WithDescription($"消息包含指令：{prefix}{command}");

    public static EventTunnelBuilder<(EventContext<TEvent> EventContext, string Text)> OnText<TEvent>(
        this EventTunnelBuilder<EventContext<TEvent>> builder
    ) where TEvent : MessageEvent =>
        builder
            .Where(ctx => ctx.Event.Message.OfType<TextData>().Any())
            .Select(ctx => (ctx, Text: string.Join(null, ctx.Event.Message.OfType<TextData>().Select(data => data.Text))))
            .WithDescription("消息包含文本");

    public static EventTunnelBuilder<(EventContext<TEvent> EventContext, string MessageId)> OnReply<TEvent>(
        this EventTunnelBuilder<EventContext<TEvent>> builder
    ) where TEvent : MessageEvent =>
        builder
            .Select(ctx => (ctx, Replies: ctx.Event.Message.OfType<ReplyData>()))
            .Where(t => t.Replies.Any())
            .Select(t => (t.ctx, t.Replies.First().Id))
            .WithDescription("消息包含对其它消息的回复");

    public static EventTunnelBuilder<(EventContext<TEvent> EventContext, Match Match)> OnRegex<TEvent>(
        this EventTunnelBuilder<EventContext<TEvent>> builder,
        Regex regex
    ) where TEvent : MessageEvent =>
        builder
            .Select(ctx => (ctx, Text: string.Join(null, ctx.Event.Message.OfType<TextData>().Select(data => data.Text.Trim()))))
            .Select(t => (t.ctx, Match: regex.Match(t.Text)))
            .Where(t => t.Match.Success)
            .WithDescription($"消息匹配正则表达式：{regex}");

    public static EventTunnelBuilder<EventContext<GroupPokeEvent>> OnPokeSelf(
        this EventTunnelBuilder<EventContext<GroupPokeEvent>> builder,
        long selfUin
    ) =>
        builder
            .Where(ctx => ctx.Event.TargetId == selfUin)
            .WithDescription("自身在群聊中被戳一戳");

    public static EventTunnelBuilder<(EventContext<TEvent> EventContext, JsonNode? Json)> OnJson<TEvent>(
        this EventTunnelBuilder<EventContext<TEvent>> builder
    ) where TEvent : MessageEvent =>
        builder
            .Select(ctx => (ctx, Jsons: ctx.Event.Message.OfType<JsonData>()))
            .Where(t => t.Jsons.Any())
            .Select(t => (t.ctx, JsonNode.Parse(t.Jsons.First().Content)))
            .WithDescription("消息包含 Json 卡片");

    public static FunctionBuilder DoExpensive<TOut>(
        this EventTunnelBuilder<TOut> builder,
        Func<TOut, Task<bool>> something,
        Func<TOut, EventContext<GroupMessageEvent>> eventSelector,
        FunctionContext context
    ) =>
        builder.Do(async data =>
        {
            var (e, t) = eventSelector(data);
            try
            {
                await new SetGroupReactionRequest(e.GroupId, e.MessageId, "128164", true).SendAsync(context, t);
                await new SetGroupReactionRequest(e.GroupId, e.MessageId, await something(data) ? "10024" : "128293", true).SendAsync(context, t);
                await new SetGroupReactionRequest(e.GroupId, e.MessageId, "128164", false).SendAsync(context, t);
            }
            catch
            {
                await ((Task)new SetGroupReactionRequest(e.GroupId, e.MessageId, "128293", true)
                    .SendAsync(context, t)).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
                throw;
            }
        });
}
