namespace Robin.Abstractions.Operation.Responses;

public record SendGroupMessageResponse(
    bool Success,
    int ReturnCode,
    string? ErrorMessage,
    int? MessageId
) : Response(Success, ReturnCode, ErrorMessage);
