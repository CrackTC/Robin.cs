using Robin.Abstractions.Message.Entities;

namespace Robin.Abstractions.Operation.Requests;

public record SendForwardMessageRequest(List<CustomNodeData> Messages) : Request;