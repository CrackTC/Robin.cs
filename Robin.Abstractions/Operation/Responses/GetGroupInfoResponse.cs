using Robin.Abstractions.Entity;

namespace Robin.Abstractions.Operation.Responses;

public record GetGroupInfoResponse(GroupInfo Info) : Response;
