using System.Reflection;

namespace ArgumentBase;

public class ArgFlagInfo
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required PropertyInfo Property { get; init; }

    internal ArgFlagInfo() { }
}
