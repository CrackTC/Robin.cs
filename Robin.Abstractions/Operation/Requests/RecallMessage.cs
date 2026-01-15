namespace Robin.Abstractions.Operation.Requests;

public record RecallMessage(string MessageId) : RequestFor<Response>;
