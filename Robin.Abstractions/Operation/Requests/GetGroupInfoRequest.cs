using Robin.Abstractions.Operation.Responses;

namespace Robin.Abstractions.Operation.Requests;

public record GetGroupInfoRequest(long GroupId, bool NoCache = false) : RequestFor<GetGroupInfoResponse>;
