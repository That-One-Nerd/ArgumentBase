using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ArgumentBase;

#pragma warning disable IL2070, IL2090 // Shut up.

public abstract class ArgumentBase<TSelf> where TSelf : ArgumentBase<TSelf>, new()
{
    public static ReadOnlyCollection<ArgParameterInfo> Parameters { get; private set; }
    public static ReadOnlyCollection<ArgVariableInfo> Variables { get; private set; }
    public static ReadOnlyCollection<ArgFlagInfo> Flags { get; private set; }

    public static ReadOnlyDictionary<string, string> ParameterDescriptions { get; private set; }
    public static ReadOnlyDictionary<string, string> VariableDescriptions { get; private set; }
    public static ReadOnlyDictionary<string, string> FlagDescriptions { get; private set; }
    public static ReadOnlyDictionary<string, PropertyInfo> VariableTable { get; private set; }
    public static ReadOnlyDictionary<string, PropertyInfo> FlagTable { get; private set; }

    static ArgumentBase()
    {
        IEnumerable<PropertyInfo> allProps = typeof(TSelf).GetProperties().Where(x => x.SetMethod is not null);

        List<ArgParameterInfo> parameters = [];
        List<ArgVariableInfo> variables = [];
        List<ArgFlagInfo> flags = [];

        Dictionary<string, string> paramDesc = [], varDesc = [], flagDesc = [];
        Dictionary<string, PropertyInfo> varTable = [], flagTable = [];
        foreach (PropertyInfo prop in allProps)
        {
            IsParameterAttribute? paramAtt = prop.GetCustomAttribute<IsParameterAttribute>();
            IsVariableAttribute? varAtt = prop.GetCustomAttribute<IsVariableAttribute>();
            IsFlagAttribute? flagAtt = prop.GetCustomAttribute<IsFlagAttribute>();

            if (paramAtt is not null)
            {
                ArgParameterInfo info = new()
                {
                    Order = paramAtt.order,
                    Name = paramAtt.name ?? prop.Name,
                    Description = paramAtt.description,
                    Property = prop
                };
                parameters.Add(info);

                string trueName = info.Name.Trim();
                paramDesc.Add(trueName, info.Description);
            }
            if (varAtt is not null)
            {
                ArgVariableInfo info = new()
                {
                    Name = varAtt.name ?? prop.Name,
                    Description = varAtt.description,
                    Property = prop
                };
                variables.Add(info);

                string trueName = $"-{info.Name.Trim()}";
                varDesc.Add(trueName, info.Description);
                varTable.Add(trueName, info.Property);
            }
            if (flagAtt is not null)
            {
                ArgFlagInfo info = new()
                {
                    Name = flagAtt.name ?? prop.Name,
                    Description = flagAtt.description,
                    Property = prop
                };
                flags.Add(info);

                string trueName = $"--{info.Name.Trim()}";
                flagDesc.Add(trueName, info.Description);
                flagTable.Add(trueName, info.Property);
            }
        }

        parameters.Sort((a, b) => a.Order.CompareTo(b.Order));
        Parameters = parameters.AsReadOnly();
        Variables = variables.AsReadOnly();
        Flags = flags.AsReadOnly();

        // I would sort these, but I would need yet another for loop to do that.
        ParameterDescriptions = paramDesc.AsReadOnly();
        VariableDescriptions = varDesc.AsReadOnly();
        FlagDescriptions = flagDesc.AsReadOnly();
        VariableTable = varTable.AsReadOnly();
        FlagTable = flagTable.AsReadOnly();
    }

    public static TSelf Parse(string[] args)
    {
        TSelf result = new();
        if (args.Length == 0) return result;

        result.anyArguments = true;
        int parameterIndex = 0;
        List<string> unknownParams = [], unknownVars = [], unknownFlags = [];
        List<string> badValParams = [], badValVars = [];
        foreach (string arg in args)
        {
            if (arg.StartsWith("--"))      // Flag
            {
                string name = arg[2..];
                ArgFlagInfo? flag = Flags.SingleOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

                if (flag is null)
                {
                    // Unknown flag.
                    unknownFlags.Add(name);
                    continue;
                }

                // Flip flag.
                bool original = (bool)flag.Property.GetValue(result)!;
                flag.Property.SetValue(result, !original);
                result.anyFlags = true;
            }
            else if (arg.StartsWith('-'))  // Variable
            {
                int splitter = arg.IndexOf(':');
                string name = arg[1..splitter], valueStr = arg[(splitter + 1)..];
                ArgVariableInfo? var = Variables.SingleOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

                if (var is null)
                {
                    // Unknown variable.
                    unknownVars.Add(name);
                    continue;
                }

                object? value = parseObject(valueStr, var.Property.PropertyType);
                if (value is null)
                {
                    // Issue parsing the object.
                    badValVars.Add(name);
                    continue;
                }
                var.Property.SetValue(result, value);
                result.anyVars = true;
            }
            else                           // Parameter
            {
                if (parameterIndex >= Parameters.Count)
                {
                    // Too many parameters.
                    unknownParams.Add(arg);
                    continue;
                }
                ArgParameterInfo param = Parameters[parameterIndex];
                parameterIndex++;

                object? value = parseObject(arg, param.Property.PropertyType);
                if (value is null)
                {
                    // Issue parsing the object.
                    badValParams.Add(arg);                    
                    continue;
                }
                param.Property.SetValue(result, value);
                result.anyParams = true;
            }
        }

        int unknownArgs = unknownParams.Count + unknownVars.Count + unknownFlags.Count;
        if (unknownArgs > 0)
        {
            // Unknown arguments.
            StringBuilder warnMsg = new();
            warnMsg.Append($"  \x1b[3;33m{unknownArgs} {(unknownArgs == 1 ? "argument was" : "arguments were")} not recognized:\n  ");
            int lineLen = 2, maxLineLen = (int)(0.65 * Console.WindowWidth - 1);

            foreach (string badParam in unknownParams)
            {
                if (lineLen + badParam.Length + 3 > maxLineLen)
                {
                    warnMsg.Append("\n  ");
                    lineLen = 2;
                }
                warnMsg.Append($"\"\x1b[37m{badParam}\x1b[33m\" ");
                lineLen += badParam.Length + 3;
            }
            foreach (string badVar in unknownVars)
            {
                if (lineLen + badVar.Length + 4 > maxLineLen)
                {
                    warnMsg.Append("\n  ");
                    lineLen = 2;
                }
                warnMsg.Append($"\"\x1b[36m-{badVar}\x1b[33m\" ");
                lineLen += badVar.Length + 4;
            }
            foreach (string badFlag in unknownFlags)
            {
                if (lineLen + badFlag.Length + 5 > maxLineLen)
                {
                    warnMsg.Append("\n  ");
                    lineLen = 2;
                }
                warnMsg.Append($"\"\x1b[90m--{badFlag}\x1b[33m\" ");
                lineLen += badFlag.Length + 5;
            }
            warnMsg.AppendLine("\x1b[0m");
            Console.WriteLine(warnMsg);
        }
        int badValArgs = badValParams.Count + badValVars.Count;
        if (badValArgs > 0)
        {
            // Issue parsing arguments.
            StringBuilder warnMsg = new();
            warnMsg.Append($"  \x1b[3;33m{badValArgs} {(badValArgs == 1 ? "value" : "values")} couldn't be parsed:\n  ");
            int lineLen = 2, maxLineLen = (int)(0.65 * Console.WindowWidth - 1);

            foreach (string badParam in badValParams)
            {
                if (lineLen + badParam.Length + 3 > maxLineLen)
                {
                    warnMsg.Append("\n  ");
                    lineLen = 2;
                }
                warnMsg.Append($"\"\x1b[37m{badParam}\x1b[33m\" ");
                lineLen += badParam.Length + 3;
            }
            foreach (string badVar in badValVars)
            {
                if (lineLen + badVar.Length + 4 > maxLineLen)
                {
                    warnMsg.Append("\n  ");
                    lineLen = 2;
                }
                warnMsg.Append($"\"\x1b[36m-{badVar}\x1b[33m\" ");
                lineLen += badVar.Length + 4;
            }
            warnMsg.AppendLine("\x1b[0m");
            Console.WriteLine(warnMsg);
        }

        return result;

        static object? parseObject(string value, Type desired)
        {
            if (desired == typeof(string)) return value;
            else if (desired.IsEnum) return Enum.TryParse(desired, value, true, out object? enumResult) ? enumResult : null;
            else if (desired.GetInterface("IParsable`1") is null) throw new("Type must derive from IParsable. Sorry.");
            else
            {
                // A bit large. Couldn't condense into a single lambda because I wanted to cache the parameters.
                // I have to do all this weird stuff as a whole to be able to parse any object that derives
                // from IParsable, making extra stuff easier to implement.
                MethodInfo tryParseMethod = (from x in desired.GetMethods()
                                             let parameters = x.GetParameters()
                                             let goodName = x.Name == "TryParse"
                                             let goodAttributes = x.IsPublic && x.IsStatic
                                             let goodParams = parameters.Length == 2
                                                     && parameters[0].ParameterType == typeof(string)
                                                     && parameters[1].ParameterType.GetElementType() == desired
                                             where goodName && goodAttributes && goodParams
                                             select x).Single(); // Must exist according to IParsable.

                // Output parameters are placed in the array.
                object?[] methodParams = [value, null];
                bool success = (bool)tryParseMethod.Invoke(null, methodParams)!;
                return success ? methodParams[1] : null;
            }
        }
    }
    public static void PrintParameters(string? keyFormat = null)
    {
        if (Parameters.Count == 0) return;
        else PrintHelper.PrintKeyValues("Parameters", 2, ParameterDescriptions.ToDictionary(), keyFormat: keyFormat ?? "\x1b[37m");
    }
    public static void PrintVariables()
    {
        if (Variables.Count == 0) return;
        else PrintHelper.PrintKeyValues("Variables", 2, VariableDescriptions.ToDictionary(), keyFormat: "\x1b[36m");
    }
    public static void PrintFlags()
    {
        if (Flags.Count == 0) return;
        else PrintHelper.PrintKeyValues("Flags", 2, FlagDescriptions.ToDictionary());
    }

    public bool anyArguments;
    public bool anyParams, anyVars, anyFlags;
}
