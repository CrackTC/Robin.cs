using Robin.Abstractions.Operation.Responses;

namespace Robin.Abstractions.Operation.Requests;

public record GetGroupMemberInfo(long GroupId, long UserId, bool NoCache = false)
    : RequestFor<GetGroupMemberInfoResponse>;
