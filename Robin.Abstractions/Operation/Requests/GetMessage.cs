using Robin.Abstractions.Operation.Responses;

namespace Robin.Abstractions.Operation.Requests;

public record GetMessage(string MessageId) : RequestFor<GetMessageResponse>;
