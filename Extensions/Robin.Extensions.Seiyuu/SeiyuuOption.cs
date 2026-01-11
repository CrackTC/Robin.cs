namespace Robin.Extensions.Seiyuu;

[Serializable]
public class SeiyuuOption
{
    public List<long> BannedIds { get; set; } = [];
    public Dictionary<string, HashSet<long>> GroupLimits { get; set; } = [];
}
