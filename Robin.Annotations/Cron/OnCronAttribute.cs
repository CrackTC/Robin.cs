namespace Robin.Annotations.Cron;

[AttributeUsage(AttributeTargets.Class)]
public class OnCronAttribute(string cron) : TriggerAttribute
{
    public string Cron { get; } = cron;
    public override string GetDescription() => $"cron({Cron})";
}