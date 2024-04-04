﻿namespace Robin.Annotations.Command;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class OnCommandAttribute(string command, bool at = false) : Attribute
{
    public string Command { get; } = command;
    public bool At { get; } = at;
}