using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Conqueror.Streaming.Interactive.Transport.Http.Common;

internal static class ContextValueFormatter
{
    public static string? Format(IConquerorContextData? values)
    {
        if (values is null)
        {
            return null;
        }

        return string.Join(";", values.Where(t => t.Scope == ConquerorContextDataScope.AcrossTransports).Select(p => $"{Base64Encode(p.Key)},{Base64Encode((string)p.Value)}"));
    }

    public static IReadOnlyDictionary<string, string> Parse(string value)
    {
        return value.Split(';').Select(s => s.Trim().Split(',')).ToDictionary(a => Base64Decode(a[0]), a => Base64Decode(a[1]));
    }

    public static IReadOnlyDictionary<string, string> Parse(IEnumerable<string> values)
    {
        return values.SelectMany(s => s.Split(';')).Select(s => s.Trim().Split(',')).ToDictionary(a => Base64Decode(a[0]), a => Base64Decode(a[1]));
    }

    private static string Base64Encode(string plainText)
    {
        var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
        return Convert.ToBase64String(plainTextBytes);
    }

    private static string Base64Decode(string base64EncodedData)
    {
        var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
        return Encoding.UTF8.GetString(base64EncodedBytes);
    }
}
