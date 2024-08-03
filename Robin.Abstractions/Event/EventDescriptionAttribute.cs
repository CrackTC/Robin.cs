namespace Robin.Abstractions.Event;

[AttributeUsage(AttributeTargets.Class)]
public class EventDescriptionAttribute(string description) : Attribute
{
    public string Description { get; } = description;
}