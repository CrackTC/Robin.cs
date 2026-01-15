using Robin.Implementations.OneBot.Entity.Operations;

namespace Robin.Implementations.OneBot.Converter.Operation.Requests;

internal class SetFriendAddRequest : IOneBotRequestConverter<Abstractions.Operation.Requests.SetFriendAddRequest>
{
    public OneBotRequest ConvertToOneBotRequest(Abstractions.Operation.Requests.SetFriendAddRequest request, OneBotMessageConverter _) =>
        new("set_friend_add_request", new()
        {
            ["flag"] = request.Flag,
            ["approve"] = request.Approve,
            ["remark"] = request.Remark
        });
}
