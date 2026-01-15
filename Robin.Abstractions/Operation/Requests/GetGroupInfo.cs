using Robin.Abstractions.Operation.Responses;

namespace Robin.Abstractions.Operation.Requests;

public record GetGroupInfo(long GroupId, bool NoCache = false) : RequestFor<GetGroupInfoResponse>;
