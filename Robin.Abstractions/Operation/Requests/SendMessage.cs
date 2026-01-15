using Robin.Abstractions.Message;
using Robin.Abstractions.Operation.Responses;

namespace Robin.Abstractions.Operation.Requests;

public abstract record SendMessage(MessageChain Message) : RequestFor<SendMessageResponse>;
