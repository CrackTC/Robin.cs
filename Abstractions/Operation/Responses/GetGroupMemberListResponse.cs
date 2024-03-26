using Robin.Abstractions.Operation.Entities;
using Robin.Common;

namespace Robin.Abstractions.Operation.Responses;

public record GetGroupMemberListResponse(
    bool Success,
    int ReturnCode,
    string? ErrorMessage,
    EquatableImmutableArray<GroupMemberInfo>? Members
) : Response(Success, ReturnCode, ErrorMessage);