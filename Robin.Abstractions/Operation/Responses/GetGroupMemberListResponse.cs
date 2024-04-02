using Robin.Abstractions.Entities;

namespace Robin.Abstractions.Operation.Responses;

public record GetGroupMemberListResponse(
    bool Success,
    int ReturnCode,
    string? ErrorMessage,
    List<GroupMemberInfo>? Members
) : Response(Success, ReturnCode, ErrorMessage);