using Robin.Abstractions.Operation.Requests;
using Robin.Implementations.OneBot.Entity.Operations;

namespace Robin.Implementations.OneBot.Converter.Operation.Requests;

internal class DeleteMessage : OneBotRequestConverter<RecallMessage>
{
    public override OneBotRequest ConvertToOneBotRequest(
        RecallMessage request,
        OneBotMessageConverter _
    ) => new("delete_msg", new() { ["message_id"] = Convert.ToInt32(request.MessageId) });
}
