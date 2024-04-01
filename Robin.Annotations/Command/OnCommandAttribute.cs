namespace Robin.Annotations.Command;

[AttributeUsage(AttributeTargets.Class)]
public class OnCommandAttribute(string command) : Attribute
{
    public string Command { get; set; } = command;
}