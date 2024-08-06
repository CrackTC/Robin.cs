using Robin.Abstractions.Message.Entity;

namespace Robin.Abstractions.Operation.Requests;

public record SendForwardMessageRequest(List<CustomNodeData> Messages) : Request;