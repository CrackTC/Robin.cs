using System.Text.Json;
using System.Text.Json.Nodes;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Requests;
using Robin.Implementations.OneBot.Converter;

namespace Robin.Implementations.OneBot.Entity.Operations.Requests;

using RequestType = SetFriendAddRequestRequest;

[OneBotRequest("set_friend_add_request", typeof(RequestType))]
internal class OneBotSetFriendAddRequestRequest : IOneBotRequest
{
    public JsonNode? GetJson(Request request, OneBotMessageConverter _)
    {
        if (request is not RequestType r) return null;

        return JsonSerializer.SerializeToNode(new
        {
            flag = r.Flag,
            approve = r.Approve,
            remark = r.Remark
        });
    }
}
