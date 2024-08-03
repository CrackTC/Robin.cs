using Robin.Abstractions.Event;
using Robin.Abstractions.Event.Message;
using Robin.Abstractions.Message.Entities;

namespace Robin.Annotations.Filters.Message;

public class OnCommandAttribute(string command, char prefix = '/', int filterGroup = 0) : OnMessageAttribute(filterGroup)
{
    public override bool FilterEvent(long selfId, BotEvent @event)
    {
        if (!base.FilterEvent(selfId, @event)) return false;

        var e = @event as MessageEvent;
        return e!.Message.Any(segment => segment is TextData data && data.Text
            .Trim()
            .Split(null)
            .Any(text => text == $"{prefix}{command}"));
    }

    public override string GetDescription() => $"消息包含指令：{prefix}{command}";
}
