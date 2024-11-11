namespace Robin.Abstractions.Operation.Requests;

public record GetGroupInfoRequest(long GroupId, bool NoCache = false) : Request;
