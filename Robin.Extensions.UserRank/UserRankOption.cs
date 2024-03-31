namespace Robin.Extensions.UserRank;

[Serializable]
public class UserRankOption
{
    public string Cron { get; set; } = "1 0 0 * * *";
    public int TopN { get; set; }
}