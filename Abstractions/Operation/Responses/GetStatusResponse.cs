using Robin.Abstractions.Operation.Entities;

namespace Robin.Abstractions.Operation.Responses;

public record GetStatusResponse(
    bool Success,
    int ReturnCode,
    string? ErrorMessage,
    BotStatus? Status
) : Response(Success, ReturnCode, ErrorMessage);