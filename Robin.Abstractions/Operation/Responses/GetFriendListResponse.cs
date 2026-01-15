using Robin.Abstractions.Entity;

namespace Robin.Abstractions.Operation.Responses;

public record GetFriendListResponse(List<FriendInfo> Friends) : Response;
