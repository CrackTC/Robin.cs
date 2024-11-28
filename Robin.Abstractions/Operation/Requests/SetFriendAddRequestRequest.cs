namespace Robin.Abstractions.Operation.Requests;

public record SetFriendAddRequestRequest(string Flag, bool Approve, string? Remark) : RequestFor<Response>;
