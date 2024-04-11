namespace Robin.Annotations.Cron;

[AttributeUsage(AttributeTargets.Class)]
public class OnCronAttribute(string cron) : Attribute
{
    public string Cron { get; } = cron;
}