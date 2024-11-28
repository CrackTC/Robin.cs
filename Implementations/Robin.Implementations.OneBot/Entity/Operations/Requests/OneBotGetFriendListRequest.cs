using System.Text.Json;
using System.Text.Json.Nodes;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Requests;
using Robin.Implementations.OneBot.Converter;

namespace Robin.Implementations.OneBot.Entity.Operations.Requests;

[OneBotRequest("get_friend_list", typeof(GetFriendListRequest))]
internal class OneBotGetFriendListRequest : IOneBotRequest
{
    public JsonNode? GetJson(Request request, OneBotMessageConverter _)
    {
        if (request is not GetFriendListRequest) return null;
        return JsonSerializer.SerializeToNode(new { });
    }
}
