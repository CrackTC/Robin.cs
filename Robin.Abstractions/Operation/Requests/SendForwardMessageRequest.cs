using Robin.Abstractions.Message.Entities;
using Robin.Abstractions.Common;

namespace Robin.Abstractions.Operation.Requests;

public record SendForwardMessageRequest(EquatableImmutableArray<CustomNodeData> Messages) : Request;