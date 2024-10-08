namespace Robin.Implementations.OneBot.Entity.Operations;


[AttributeUsage(AttributeTargets.Class)]
internal class OneBotResponseDataAttribute(Type requestType) : Attribute
{
    public Type RequestType { get; set; } = requestType;
}
