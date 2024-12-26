namespace Robin.Abstractions.Operation.Responses;

public record SendMessageResponse(
    bool Success,
    int ReturnCode,
    string? ErrorMessage,
    string? MessageId
) : Response(Success, ReturnCode, ErrorMessage);
