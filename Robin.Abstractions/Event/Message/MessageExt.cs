using Robin.Abstractions.Message;
using Robin.Abstractions.Operation.Requests;

namespace Robin.Abstractions.Event.Message;

public static class MessageExt
{
    public static SendMessageRequest NewMessageRequest(this MessageEvent e, MessageChain chain) => e switch
    {
        PrivateMessageEvent { SourceId: var s } => new SendPrivateMessageRequest(s, chain),
        GroupMessageEvent { SourceId: var s } => new SendGroupMessageRequest(s, chain),
        _ => throw new NotSupportedException()
    };
}
