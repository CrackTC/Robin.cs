namespace Robin.Abstractions.Operation.Requests;

public record SetGroupReactionRequest(
    long GroupId,
    string MessageId,
    string Code,
    bool IsAdd
) : RequestFor<Response>;
