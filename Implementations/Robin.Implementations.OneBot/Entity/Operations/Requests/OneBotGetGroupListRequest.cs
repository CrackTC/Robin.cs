using System.Text.Json;
using System.Text.Json.Nodes;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Requests;
using Robin.Implementations.OneBot.Converter;

namespace Robin.Implementations.OneBot.Entity.Operations.Requests;

[OneBotRequest("get_group_list", typeof(GetGroupListRequest))]
internal class OneBotGetGroupListRequest : IOneBotRequest
{
    public JsonNode? GetJson(Request request, OneBotMessageConverter _)
    {
        if (request is not GetGroupListRequest) return null;
        return JsonSerializer.SerializeToNode(new { });
    }
}
