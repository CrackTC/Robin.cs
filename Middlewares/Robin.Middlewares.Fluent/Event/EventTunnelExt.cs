using System.Text.RegularExpressions;
using Robin.Abstractions.Context;
using Robin.Abstractions.Event.Message;
using Robin.Abstractions.Event.Notice;
using Robin.Abstractions.Message.Entity;

namespace Robin.Middlewares.Fluent.Event;

public static class FluentExt
{
    public static EventTunnelBuilder<EventContext<TEvent>> OnAtSelf<TEvent>(
        this EventTunnelBuilder<EventContext<TEvent>> builder,
        long selfUin
    ) where TEvent : MessageEvent =>
        builder
            .Where(ctx => ctx.Event.Message.OfType<AtData>().Any(at => at.Uin == selfUin))
            .WithDescription("自身在群聊中被@");

    public static EventTunnelBuilder<EventContext<TEvent>> OnNotAtOthers<TEvent>(
        this EventTunnelBuilder<EventContext<TEvent>> builder,
        long selfUin
    ) where TEvent : MessageEvent =>
        builder
            .Where(ctx => ctx.Event.Message.OfType<AtData>().All(at => at.Uin == selfUin))
            .WithDescription("消息未@其他人");

    public static EventTunnelBuilder<EventContext<TEvent>> OnCommand<TEvent>(
        this EventTunnelBuilder<EventContext<TEvent>> builder,
        string command,
        char prefix = '/'
    ) where TEvent : MessageEvent =>
        builder
            .Where(ctx => ctx.Event.Message.Any(seg => seg is TextData text
                                                && text.Text.Trim().Split(null).Any(t => t == $"{prefix}{command}")))
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
}
