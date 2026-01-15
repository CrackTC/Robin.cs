using Robin.Implementations.OneBot.Entity.Operations;

namespace Robin.Implementations.OneBot.Converter.Operation.Requests;

internal class SendGroupForwardMessage
    : OneBotRequestConverter<Abstractions.Operation.Requests.SendGroupForwardMessage>
{
    public override OneBotRequest ConvertToOneBotRequest(
        Abstractions.Operation.Requests.SendGroupForwardMessage request,
        OneBotMessageConverter converter
    ) =>
        new(
            "send_group_forward_msg",
            new()
            {
                ["group_id"] = request.GroupId,
                ["messages"] = converter.SerializeToArray([.. request.Messages]),
            }
        );
}
