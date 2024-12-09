namespace Robin.Extensions.UserRank;

internal class Member
{
    public long GroupId { get; init; }
    public long UserId { get; init; }
    public uint Count { get; set; }
    public long Timestamp { get; set; }
    public uint PrevCount { get; set; }
    public long PrevTimestamp { get; set; }
}
