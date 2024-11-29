namespace Robin.Middlewares.Fluent;

[Serializable]
public class FluentOption
{
    public Dictionary<string, Dictionary<string, string>> Crons { get; set; } = [];
    public string CronDescriptionLocale { get; set; } = "zh-Hans";
}
