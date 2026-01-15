namespace Robin.Implementations.OneBot.Converter;

[AttributeUsage(AttributeTargets.Class)]
internal sealed class OneBotVariantAttribute(params string[] variants) : Attribute
{
    public string[] Variants { get; } = variants ?? [];
}
