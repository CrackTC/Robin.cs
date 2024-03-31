namespace Robin.Abstractions.Operation.Requests;

public record GetGroupMemberListRequest(long GroupId, bool NoCache = false) : Request;