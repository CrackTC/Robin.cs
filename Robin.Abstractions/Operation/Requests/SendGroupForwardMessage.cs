using Robin.Abstractions.Message.Entity;
using Robin.Abstractions.Operation.Responses;

namespace Robin.Abstractions.Operation.Requests;

public record SendGroupForwardMessage(long GroupId, List<CustomNodeData> Messages)
    : RequestFor<SendGroupForwardMessageResponse>;
