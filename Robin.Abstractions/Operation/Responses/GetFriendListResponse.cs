using Robin.Abstractions.Entity;

namespace Robin.Abstractions.Operation.Responses;

public record GetFriendListResponse(
    bool Success,
    int ReturnCode,
    string? ErrorMessage,
    List<FriendInfo> Friends
) : Response(Success, ReturnCode, ErrorMessage);
