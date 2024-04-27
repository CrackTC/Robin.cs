namespace Robin.Extensions.WordCloud;

// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength
internal class Record
{
    public long GroupId { get; init; }
    public required string Content { get; init; }
}