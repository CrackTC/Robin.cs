namespace Robin.Abstractions.Operation.Requests;

public record UploadGroupFile(
    long GroupId,
    string File,
    string Name,
    string Folder = "/"
) : RequestFor<Response>;
