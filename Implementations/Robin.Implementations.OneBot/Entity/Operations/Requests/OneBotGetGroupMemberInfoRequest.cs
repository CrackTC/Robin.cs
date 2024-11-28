using System.Text.Json;
using System.Text.Json.Nodes;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Requests;
using Robin.Implementations.OneBot.Converter;

namespace Robin.Implementations.OneBot.Entity.Operations.Requests;

using RequestType = GetGroupMemberInfoRequest;

[OneBotRequest("get_group_member_info", typeof(RequestType))]
internal class OneBotGetGroupMemberInfoRequest : IOneBotRequest
{
    public JsonNode? GetJson(Request request, OneBotMessageConverter _)
    {
        if (request is not RequestType r) return null;
        return JsonSerializer.SerializeToNode(new
        {
            group_id = r.GroupId,
            user_id = r.UserId,
            no_cache = r.NoCache
        });
    }
}
