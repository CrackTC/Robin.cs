namespace Robin.Abstractions.Operation.Requests;

public record SetFriendAddRequest(string Flag, bool Approve, string? Remark) : RequestFor<Response>;
