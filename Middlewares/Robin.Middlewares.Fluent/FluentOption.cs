namespace Robin.Middlewares.Fluent;

public class FluentOption
{
    public Dictionary<string, Dictionary<string, string>> Crons { get; set; } = [];
    public string CronDescriptionLocale { get; set; } = "zh-Hans";
}
