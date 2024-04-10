using System.Text.Json;
using System.Text.Json.Nodes;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Requests;
using Robin.Implementations.OneBot.Converters;

namespace Robin.Implementations.OneBot.Entities.Operations.Requests;

[OneBotRequest("get_group_member_info", typeof(GetGroupMemberInfoRequest))]
internal class OneBotGetGroupMemberInfoRequest : IOneBotRequest
{
    public JsonNode? GetJson(Request request, OneBotMessageConverter _)
    {
        if (request is not GetGroupMemberInfoRequest r) return null;
        return JsonSerializer.SerializeToNode(new
        {
            group_id = r.GroupId,
            user_id = r.UserId,
            no_cache = r.NoCache
        });
    }
}