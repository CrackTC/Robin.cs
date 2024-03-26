namespace Robin.Abstractions.Operation;

public abstract record Response(bool Success, int ReturnCode, string? ErrorMessage);