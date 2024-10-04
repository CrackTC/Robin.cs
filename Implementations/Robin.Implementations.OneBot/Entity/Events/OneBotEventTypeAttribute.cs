namespace Robin.Implementations.OneBot.Entity.Events;

[AttributeUsage(AttributeTargets.Class)]
internal class OneBotEventTypeAttribute(string type) : Attribute
{
    public string Type { get; } = type;
}
