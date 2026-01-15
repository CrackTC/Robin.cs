using Robin.Abstractions.Entity;

namespace Robin.Abstractions.Operation.Responses;

public record GetGroupMemberListResponse(List<GroupMemberInfo> Members) : Response;
