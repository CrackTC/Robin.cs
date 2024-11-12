namespace Robin.Implementations.OneBot.Entity.Operations;


[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
internal class OneBotResponseDataAttribute(Type requestType) : Attribute
{
    public Type RequestType { get; set; } = requestType;
}
