namespace Robin.Extensions.Jm;

[Serializable]
public class JmOption
{
    public required string ApiAddress { get; set; }
    public List<int> BannedIds { get; set; } = [];
}
