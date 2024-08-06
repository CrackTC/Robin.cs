namespace Robin.Implementations.OneBot.Entity.Events;

[AttributeUsage(AttributeTargets.Class)]
internal class OneBotPostTypeAttribute(string type) : Attribute
{
    public string Type { get; } = type;
}