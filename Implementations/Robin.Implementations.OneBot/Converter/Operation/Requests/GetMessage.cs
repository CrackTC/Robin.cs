using Robin.Implementations.OneBot.Entity.Operations;

namespace Robin.Implementations.OneBot.Converter.Operation.Requests;

internal class GetMessage : IOneBotRequestConverter<Abstractions.Operation.Requests.GetMessage>
{
    public OneBotRequest ConvertToOneBotRequest(
        Abstractions.Operation.Requests.GetMessage request,
        OneBotMessageConverter _
    ) => new("get_msg", new() { ["message_id"] = Convert.ToInt32(request.MessageId) });
}
