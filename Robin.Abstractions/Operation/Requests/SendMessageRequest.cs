using Robin.Abstractions.Message;
using Robin.Abstractions.Operation.Responses;

namespace Robin.Abstractions.Operation.Requests;

public abstract record SendMessageRequest(MessageChain Message) : RequestFor<SendMessageResponse>;
