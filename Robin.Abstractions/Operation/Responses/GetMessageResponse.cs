using Robin.Abstractions.Entity;

namespace Robin.Abstractions.Operation.Responses;

public record GetMessageResponse(
    bool Success,
    int ReturnCode,
    string? ErrorMessage,
    MessageInfo? Message
) : Response(Success, ReturnCode, ErrorMessage);
