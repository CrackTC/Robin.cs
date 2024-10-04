using Robin.Abstractions.Message;

namespace Robin.Abstractions.Operation.Requests;

public record SendGroupMessageRequest(long GroupId, MessageChain Message) : Request;
