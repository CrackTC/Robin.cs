namespace Robin.Annotations.Cron;

[AttributeUsage(AttributeTargets.Class)]
public class OnCronAttribute(string cron) : TriggerAttribute
{
    public string Cron { get; } = cron;

    private static readonly CronExpressionDescriptor.Options _options = new()
    {
        Use24HourTimeFormat = true,
        Locale = "zh-Hans"
    };
    public override string GetDescription()
        => $"{CronExpressionDescriptor.ExpressionDescriptor.GetDescription(Cron, _options)} 自动触发";
}