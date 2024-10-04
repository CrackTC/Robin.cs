namespace Robin.Abstractions.Communication;

// For keyed service registration
[AttributeUsage(AttributeTargets.Class)]
public sealed class BackendAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}
