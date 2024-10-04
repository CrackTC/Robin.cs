namespace Robin.Implementations.OneBot.Entity.Operations;

[AttributeUsage(AttributeTargets.Class)]
internal class OneBotRequestAttribute(string endpoint, Type type) : Attribute
{
    public string Endpoint { get; set; } = endpoint;
    public Type Type { get; set; } = type;
}
