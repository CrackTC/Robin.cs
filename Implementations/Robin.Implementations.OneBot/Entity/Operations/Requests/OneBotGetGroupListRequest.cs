using System.Text.Json;
using System.Text.Json.Nodes;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Requests;
using Robin.Implementations.OneBot.Converter;

namespace Robin.Implementations.OneBot.Entity.Operations.Requests;

using RequestType = GetGroupListRequest;

[OneBotRequest("get_group_list", typeof(RequestType))]
internal class OneBotGetGroupListRequest : IOneBotRequest
{
    public JsonNode? GetJson(Request request, OneBotMessageConverter _)
    {
        if (request is not RequestType) return null;
        return JsonSerializer.SerializeToNode(new { });
    }
}
