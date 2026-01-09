namespace Robin.Extensions.Seiyuu;

[Serializable]
public class SeiyuuOption
{
    public List<long> BannedIds { get; set; } = [];
    public Dictionary<string, List<long>> GroupLimits { get; set; } = [];
}
