namespace Robin.Abstractions.Operation.Requests;

public record UploadGroupFileRequest(
    long GroupId,
    string File,
    string Name,
    string Folder = "/"
) : RequestFor<Response>;
