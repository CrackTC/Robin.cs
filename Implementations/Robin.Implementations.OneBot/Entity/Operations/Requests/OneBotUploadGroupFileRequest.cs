using System.Text.Json;
using System.Text.Json.Nodes;
using Robin.Abstractions.Operation;
using Robin.Abstractions.Operation.Requests;
using Robin.Implementations.OneBot.Converter;

namespace Robin.Implementations.OneBot.Entity.Operations.Requests;

using RequestType = UploadGroupFileRequest;

[OneBotRequest("upload_group_file", typeof(RequestType))]
internal class OneBotUploadGroupFileRequest : IOneBotRequest
{
    public JsonNode? GetJson(Request request, OneBotMessageConverter _)
    {
        if (request is not RequestType r) return null;

        return JsonSerializer.SerializeToNode(new
        {
            group_id = r.GroupId,
            file = r.File,
            name = r.Name,
            folder = r.Folder
        });
    }
}
