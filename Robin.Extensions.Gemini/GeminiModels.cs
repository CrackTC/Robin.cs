using Robin.Extensions.Gemini.Entities;

namespace Robin.Extensions.Gemini;

// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength
internal class User
{
    public long UserId { get; init; }
    public required string ModelName { get; set; }
    public required string SystemCommand { get; set; }
    public List<Message> Messages { get; init; } = [];
}

internal class Message
{
    public required User User { get; init; }
    public GeminiRole Role { get; init; }
    public required string Content { get; init; }
    public long Timestamp { get; init; }
}