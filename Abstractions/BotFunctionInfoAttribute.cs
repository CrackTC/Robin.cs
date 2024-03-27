namespace Robin.Abstractions;

[AttributeUsage(AttributeTargets.Class)]
public class BotFunctionInfoAttribute(string name, string description = "", bool enabled = true, params Type[] eventTypes) : Attribute
{
    public string Name => name;
    public Type[] EventTypes { get; } = eventTypes;
    public string Description => description;
}