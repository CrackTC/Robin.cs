using Robin.Implementations.OneBot.Entity.Operations;

namespace Robin.Implementations.OneBot.Converter.Operation.Requests;

internal class SendGroupMessage : IOneBotRequestConverter<Abstractions.Operation.Requests.SendGroupMessage>
{
    public OneBotRequest ConvertToOneBotRequest(Abstractions.Operation.Requests.SendGroupMessage request, OneBotMessageConverter converter) =>
        new("send_group_msg", new()
        {
            ["group_id"] = request.GroupId,
            ["message"] = converter.SerializeToArray(request.Message),
        });
}
