namespace Robin.Extensions.Oa;

[Serializable]
public class OaOption
{
    public required List<long> Groups { get; set; }
    public required long TempGroup { get; set; }
    public required bool UseVpn { get; set; }
    public string? VpnUsername { get; set; }
    public string? VpnPassword { get; set; }
}
