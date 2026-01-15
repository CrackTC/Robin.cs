using Robin.Implementations.OneBot.Entity.Operations;

namespace Robin.Implementations.OneBot.Converter.Operation.Requests;

internal class SetGroupAddRequest
    : IOneBotRequestConverter<Abstractions.Operation.Requests.SetGroupAddRequest>
{
    public OneBotRequest ConvertToOneBotRequest(
        Abstractions.Operation.Requests.SetGroupAddRequest request,
        OneBotMessageConverter _
    ) =>
        new(
            "set_group_add_request",
            new()
            {
                ["flag"] = request.Flag,
                ["sub_type"] = request.SubType,
                ["approve"] = request.Approve,
                ["reason"] = request.Reason,
            }
        );
}
