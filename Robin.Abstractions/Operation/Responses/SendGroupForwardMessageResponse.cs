using Robin.Abstractions.Entity;

namespace Robin.Abstractions.Operation.Responses;

public record SendGroupForwardMessageResponse(ForwardResult Result) : Response;
