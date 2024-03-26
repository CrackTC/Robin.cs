using Robin.Abstractions.Message;

namespace Robin.Abstractions.Operation.Requests;

public record SendPrivateMessageRequest(long UserId, MessageChain Message) : Request;