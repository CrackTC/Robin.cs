using Robin.Abstractions.Message.Entity;
using Robin.Abstractions.Operation.Responses;

namespace Robin.Abstractions.Operation.Requests;

public record SendForwardMessageRequest(List<CustomNodeData> Messages) : RequestFor<SendForwardMessageResponse>;
