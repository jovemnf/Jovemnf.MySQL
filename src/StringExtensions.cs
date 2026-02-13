using System;

namespace Jovemnf.MySQL;

internal static class StringExtensions
{
    public static string ToSnakeCase(this string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return s;

        // Implementation similar to the one in Program.cs
        var buffer = new char[s.Length * 2];
        var j = 0;

        for (var i = 0; i < s.Length; i++)
        {
            var c = s[i];
            if (char.IsUpper(c))
            {
                if (i > 0) buffer[j++] = '_';
                buffer[j++] = char.ToLowerInvariant(c);
            }
            else
            {
                buffer[j++] = c;
            }
        }

        return new string(buffer, 0, j);
    }
}
