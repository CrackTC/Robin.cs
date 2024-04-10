namespace Robin.Implementations.OneBot.Entities.Events;

[AttributeUsage(AttributeTargets.Class)]
internal class OneBotPostTypeAttribute(string type) : Attribute
{
    public string Type { get; } = type;
}