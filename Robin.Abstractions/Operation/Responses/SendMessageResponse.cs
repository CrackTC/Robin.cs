namespace Robin.Abstractions.Operation.Responses;

public record SendMessageResponse(
    bool Success,
    int ReturnCode,
    string? ErrorMessage,
    int? MessageId
) : Response(Success, ReturnCode, ErrorMessage);
