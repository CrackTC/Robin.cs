using Robin.Implementations.OneBot.Entity.Operations;

namespace Robin.Implementations.OneBot.Converter.Operation.Requests;

internal class SendPrivateMessage : IOneBotRequestConverter<Abstractions.Operation.Requests.SendPrivateMessage>
{
    public OneBotRequest ConvertToOneBotRequest(Abstractions.Operation.Requests.SendPrivateMessage request, OneBotMessageConverter converter) =>
        new("send_private_msg", new()
        {
            ["user_id"] = request.UserId,
            ["message"] = converter.SerializeToArray(request.Message),
        });
}
