using Robin.Abstractions.Message;
using Robin.Abstractions.Operation.Requests;

namespace Robin.Abstractions.Event.Message;

public static class MessageExt
{
    public static SendMessage NewMessageRequest(this MessageEvent e, MessageChain chain) =>
        e switch
        {
            PrivateMessageEvent { SourceId: var s } => new SendPrivateMessage(s, chain),
            GroupMessageEvent { SourceId: var s } => new SendGroupMessage(s, chain),
            _ => throw new NotSupportedException(),
        };
}
