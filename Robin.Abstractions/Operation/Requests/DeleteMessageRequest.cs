using Robin.Abstractions.Operation.Responses;

namespace Robin.Abstractions.Operation.Requests;

public record DeleteMessageRequest(string MessageId) : RequestFor<Response>;
