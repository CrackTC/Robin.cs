namespace Robin.Implementations.OneBot.Entities.Message.Data;

[AttributeUsage(AttributeTargets.Class)]
internal class OneBotSegmentDataAttribute(string typeName, params Type[] types) : Attribute
{
    public string TypeName { get; } = typeName;
    public IEnumerable<Type> Types { get; } = types;
}