using System.Reflection;

namespace ArgumentBase;

public class ArgVariableInfo
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required PropertyInfo Property { get; init; }

    internal ArgVariableInfo() { }
}
