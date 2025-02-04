using System.Reflection;

namespace ArgumentBase;

public class ArgParameterInfo
{
    public required int Order { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required PropertyInfo Property { get; init; }

    internal ArgParameterInfo() { }
}
