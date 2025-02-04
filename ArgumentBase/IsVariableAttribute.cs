using System;

namespace ArgumentBase;

[AttributeUsage(AttributeTargets.Property)]
public class IsVariableAttribute : Attribute
{
    public readonly string description;
    public readonly string? name;

    public IsVariableAttribute(string description)
    {
        this.description = description;
        name = null;
    }
    public IsVariableAttribute(string name, string description)
    {
        this.description = description;
        this.name = name;
    }
}
