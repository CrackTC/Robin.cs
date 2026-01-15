using Robin.Abstractions.Entity;

namespace Robin.Abstractions.Operation.Responses;

public record GetMessageResponse(MessageInfo Message) : Response;
