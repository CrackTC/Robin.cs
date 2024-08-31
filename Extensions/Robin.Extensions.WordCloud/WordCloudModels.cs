namespace Robin.Extensions.WordCloud;

// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength
internal class Record
{
    public long RecordId { get; init; }
    public long GroupId { get; init; }
    public required string Content { get; init; }
}