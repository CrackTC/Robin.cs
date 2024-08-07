using Robin.Abstractions.Context;
using Robin.Abstractions.Event;
using Robin.Abstractions.Event.Message;
using Robin.Abstractions.Message.Entity;

namespace Robin.Annotations.Filters.Message;

public class OnCommandAttribute(
    string command,
    char prefix = '/',
    int filterGroup = 0
) : BaseEventFilterAttribute(filterGroup)
{
    public override bool FilterEvent(EventContext<BotEvent> eventContext)
    {
        if (eventContext.Event is not MessageEvent e) return false;
        return e.Message.Any(segment => segment is TextData data && data.Text
            .Trim()
            .Split(null)
            .Any(text => text == $"{prefix}{command}"));
    }

    public override string GetDescription() => $"消息包含指令：{prefix}{command}";
}
