using System;

namespace ArgumentBase;

[AttributeUsage(AttributeTargets.Property)]
public class IsParameterAttribute : Attribute
{
    public readonly int order;
    public readonly string description;
    public readonly string? name;

    public IsParameterAttribute(int order, string description)
    {
        this.order = order;
        this.description = description;
        name = null;
    }
    public IsParameterAttribute(int order, string name, string description)
    {
        this.order = order;
        this.description = description;
        this.name = name;
    }
}
