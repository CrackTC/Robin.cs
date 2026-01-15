using Robin.Implementations.OneBot.Entity.Operations;

namespace Robin.Implementations.OneBot.Converter.Operation.Requests;

internal class SendGroupForwardMessage : IOneBotRequestConverter<Abstractions.Operation.Requests.SendGroupForwardMessage>
{
    public OneBotRequest ConvertToOneBotRequest(Abstractions.Operation.Requests.SendGroupForwardMessage request, OneBotMessageConverter converter) =>
        new("send_group_forward_msg", new()
        {
            ["group_id"] = request.GroupId,
            ["messages"] = converter.SerializeToArray([.. request.Messages])
        });
}
