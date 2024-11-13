using Robin.Abstractions.Entity;

namespace Robin.Abstractions.Operation.Responses;

public record SendGroupForwardMessageResponse(
    bool Success,
    int ReturnCode,
    string? ErrorMessage,
    ForwardResult? Result
) : Response(Success, ReturnCode, ErrorMessage);
