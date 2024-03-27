namespace Robin.Abstractions.Operation.Requests;

public record GetGroupMemberInfoRequest(long GroupId, long UserId, bool NoCache = false) : Request;