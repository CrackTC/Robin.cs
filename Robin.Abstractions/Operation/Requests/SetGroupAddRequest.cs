namespace Robin.Abstractions.Operation.Requests;

public record SetGroupAddRequest(string Flag, string SubType, bool Approve, string? Reason) : RequestFor<Response>;
