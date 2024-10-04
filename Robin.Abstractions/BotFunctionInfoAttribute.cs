namespace Robin.Abstractions;

[AttributeUsage(AttributeTargets.Class)]
public sealed class BotFunctionInfoAttribute(string name, string description = "", params Type[] eventTypes) : Attribute
{
    public string Name => name;
    public IEnumerable<Type> EventTypes { get; } = eventTypes;
    public string Description => description;
}
