using Robin.Abstractions.Common;
using Robin.Abstractions.Entities;

namespace Robin.Abstractions.Operation.Responses;

public record GetGroupMemberListResponse(
    bool Success,
    int ReturnCode,
    string? ErrorMessage,
    EquatableImmutableArray<GroupMemberInfo>? Members
) : Response(Success, ReturnCode, ErrorMessage);