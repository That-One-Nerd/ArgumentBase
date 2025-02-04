using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ArgumentBase;

internal static class PrintHelper
{
    public static void PrintKeyValues(string title, int indent, Dictionary<string, string> values,
                                      string? keyFormat = null, string? separatorFormat = null, string? valueFormat = null)
    {
        StringBuilder result = new();
        result.Append($"{new string(' ', indent)}\x1b[1;97m{title}:\x1b[22m\n");

        int maxLength = 0;
        StringBuilder[] lines = new StringBuilder[values.Count];
        IEnumerator<KeyValuePair<string, string>> iterator = values.GetEnumerator();
        for (int i = 0; i < values.Count; i++)
        {
            iterator.MoveNext();
            KeyValuePair<string, string> kv = iterator.Current;
            lines[i] = new StringBuilder().Append($"{new string(' ', indent + 2)}{keyFormat ?? "\x1b[90m"}{kv.Key}");

            int rawKeyLength = visibleStringLength(kv.Key);
            if (rawKeyLength > maxLength) maxLength = kv.Key.Length;
        }

        int desired = maxLength + 2;
        iterator.Reset();
        for (int i = 0; i < values.Count; i++)
        {
            iterator.MoveNext();
            KeyValuePair<string, string> kv = iterator.Current;
            int rawKeyLength = visibleStringLength(kv.Key);
            int remaining = desired - rawKeyLength;

            lines[i].Append($"{new string(' ', remaining)}{separatorFormat ?? "\x1b[91m- "}{valueFormat ?? "\x1b[37m"}{kv.Value}\x1b[0m");
            result.Append(lines[i]);
            result.AppendLine();
        }
        Console.WriteLine(result);

        static int visibleStringLength(string str) => str.ToCharArray().Where(x => !char.IsControl(x)).Count();
    }
}
