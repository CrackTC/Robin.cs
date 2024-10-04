namespace Robin.Abstractions.Operation.Responses;

public record SendForwardMessageResponse(
    bool Success,
    int ReturnCode,
    string? ErrorMessage,
    string? ResId
) : Response(Success, ReturnCode, ErrorMessage);
