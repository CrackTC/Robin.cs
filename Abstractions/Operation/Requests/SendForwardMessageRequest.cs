using Robin.Abstractions.Message;
using Robin.Abstractions.Message.Entities;
using Robin.Common;

namespace Robin.Abstractions.Operation.Requests;

public record SendForwardMessageRequest(EquatableImmutableArray<NodeData> Messages) : Request;