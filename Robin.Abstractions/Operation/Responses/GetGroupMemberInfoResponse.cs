using Robin.Abstractions.Entity;

namespace Robin.Abstractions.Operation.Responses;

public record GetGroupMemberInfoResponse(GroupMemberInfo Info) : Response;
