namespace Robin.Abstractions.Operation.Requests;

public record SetGroupReaction(
    long GroupId,
    string MessageId,
    string Code,
    bool IsAdd
) : RequestFor<Response>;
