namespace Robin.Implementations.OneBot.Entities.Events;

[AttributeUsage(AttributeTargets.Class)]
internal class OneBotEventTypeAttribute(string type) : Attribute
{
    public string Type { get; } = type;
}