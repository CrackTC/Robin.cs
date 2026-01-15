using Robin.Implementations.OneBot.Entity.Operations;

namespace Robin.Implementations.OneBot.Converter.Operation.Requests;

internal class RecallMessage
    : IOneBotRequestConverter<Abstractions.Operation.Requests.RecallMessage>
{
    public OneBotRequest ConvertToOneBotRequest(
        Abstractions.Operation.Requests.RecallMessage request,
        OneBotMessageConverter _
    ) => new("delete_msg", new() { ["message_id"] = Convert.ToInt32(request.MessageId) });
}
