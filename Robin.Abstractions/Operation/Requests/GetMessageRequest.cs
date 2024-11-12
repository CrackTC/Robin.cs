using Robin.Abstractions.Operation.Responses;

namespace Robin.Abstractions.Operation.Requests;

public record GetMessageRequest(string MessageId) : RequestFor<GetMessageResponse>;
