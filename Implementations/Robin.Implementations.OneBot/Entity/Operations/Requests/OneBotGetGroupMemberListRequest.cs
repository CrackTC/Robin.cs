using System.Text.Json;
using System.Text.Json.Nodes;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Requests;
using Robin.Implementations.OneBot.Converter;

namespace Robin.Implementations.OneBot.Entity.Operations.Requests;

using RequestType = GetGroupMemberListRequest;

[OneBotRequest("get_group_member_list", typeof(RequestType))]
internal class OneBotGetGroupMemberListRequest : IOneBotRequest
{
    public JsonNode? GetJson(Request request, OneBotMessageConverter _)
    {
        if (request is not RequestType r) return null;
        return JsonSerializer.SerializeToNode(new
        {
            group_id = r.GroupId,
            no_cache = r.NoCache
        });
    }
}
