using Robin.Abstractions.Entity;

namespace Robin.Abstractions.Operation.Responses;

public record GetGroupListResponse(List<GroupInfo> Groups) : Response;
