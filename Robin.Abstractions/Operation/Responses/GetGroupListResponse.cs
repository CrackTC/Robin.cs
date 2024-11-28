using Robin.Abstractions.Entity;

namespace Robin.Abstractions.Operation.Responses;

public record GetGroupListResponse(
    bool Success,
    int ReturnCode,
    string? ErrorMessage,
    List<GroupInfo> Groups
) : Response(Success, ReturnCode, ErrorMessage);
