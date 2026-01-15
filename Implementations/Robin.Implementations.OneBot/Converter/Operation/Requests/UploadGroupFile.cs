using Robin.Implementations.OneBot.Entity.Operations;

namespace Robin.Implementations.OneBot.Converter.Operation.Requests;

internal class UploadGroupFile
    : IOneBotRequestConverter<Abstractions.Operation.Requests.UploadGroupFile>
{
    public OneBotRequest ConvertToOneBotRequest(
        Abstractions.Operation.Requests.UploadGroupFile request,
        OneBotMessageConverter _
    ) =>
        new(
            "upload_group_file",
            new()
            {
                ["group_id"] = request.GroupId,
                ["file"] = request.File,
                ["name"] = request.Name,
                ["folder"] = request.Folder,
            }
        );
}
