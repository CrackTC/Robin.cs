using Robin.Abstractions.Entities;

namespace Robin.Abstractions.Operation.Responses;

public record GetGroupMemberInfoResponse(
    bool Success,
    int ReturnCode,
    string? ErrorMessage,
    GroupMemberInfo? Info
) : Response(Success, ReturnCode, ErrorMessage);