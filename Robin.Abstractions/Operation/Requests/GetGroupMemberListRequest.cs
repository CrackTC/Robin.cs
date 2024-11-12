using Robin.Abstractions.Operation.Responses;

namespace Robin.Abstractions.Operation.Requests;

public record GetGroupMemberListRequest(long GroupId, bool NoCache = false) : RequestFor<GetGroupMemberListResponse>;
