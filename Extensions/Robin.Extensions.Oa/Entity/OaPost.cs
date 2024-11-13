namespace Robin.Extensions.Oa.Entity;

internal class OaPost
{
    public required int Id { get; init; }
    public required string Title { get; init; }
    public required DateTime DateTime { get; init; }
    public required string Source { get; init; }
    public required string Content { get; init; }
    public required List<Uri> Images { get; init; }
    public required List<string> DataImages { get; init; }
    public required List<OaAttachment> Attachments { get; init; }
}
