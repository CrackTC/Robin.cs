namespace Robin.Extensions.WordCloud;

internal class Record
{
    public long RecordId { get; init; }
    public long GroupId { get; init; }
    public required string Content { get; init; }
}
