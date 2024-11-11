using Robin.Abstractions.Entity;

namespace Robin.Abstractions.Operation.Responses;

public record GetGroupInfoResponse(
    bool Success,
    int ReturnCode,
    string? ErrorMessage,
    GroupInfo? Info
) : Response(Success, ReturnCode, ErrorMessage);
