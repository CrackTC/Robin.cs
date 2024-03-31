namespace Robin.Abstractions.Operation;

public record Response(bool Success, int ReturnCode, string? ErrorMessage);