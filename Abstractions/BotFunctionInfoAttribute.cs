namespace Robin.Abstractions;

[AttributeUsage(AttributeTargets.Class)]
public class BotFunctionInfoAttribute(string name, Type? eventType = null, string description = "", bool enabled = true) : Attribute
{
    public string Name => name;
    public Type? EventType { get; } = eventType;
    public string Description => description;
    public bool Enabled => enabled;
}