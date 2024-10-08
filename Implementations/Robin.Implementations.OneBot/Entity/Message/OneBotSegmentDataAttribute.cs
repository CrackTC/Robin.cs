namespace Robin.Implementations.OneBot.Entity.Message;

[AttributeUsage(AttributeTargets.Class)]
internal sealed class OneBotSegmentDataAttribute(string typeName, params Type[] types) : Attribute
{
    public string TypeName { get; } = typeName;
    public IEnumerable<Type> Types { get; } = types;
}
