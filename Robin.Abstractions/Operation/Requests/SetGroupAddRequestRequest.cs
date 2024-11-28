namespace Robin.Abstractions.Operation.Requests;

public record SetGroupAddRequestRequest(string Flag, string SubType, bool Approve, string? Reason) : RequestFor<Response>;
