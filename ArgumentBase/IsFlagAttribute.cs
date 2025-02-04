using System;

namespace ArgumentBase;

[AttributeUsage(AttributeTargets.Property)]
public class IsFlagAttribute : Attribute
{
    public readonly string description;
    public readonly string? name;

    public IsFlagAttribute(string description)
    {
        this.description = description;
        name = null;
    }
    public IsFlagAttribute(string name, string description)
    {
        this.description = description;
        this.name = name;
    }
}
