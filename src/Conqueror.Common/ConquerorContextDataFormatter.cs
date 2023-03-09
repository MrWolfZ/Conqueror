using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Conqueror.Common;

/// <summary>
///     A helper class to format and parse Conqueror context data as and from a string.
/// </summary>
public static class ConquerorContextDataFormatter
{
    /// <summary>
    ///     Format all context data with scope <see cref="ConquerorContextDataScope.AcrossTransports" /> as a string.
    /// </summary>
    /// <param name="data">The context data to format</param>
    /// <returns>The formatted data if any, otherwise <c>null</c></returns>
    public static string? Format(IConquerorContextData? data)
    {
        if (data is null)
        {
            return null;
        }

        var value = Format(data.Where(t => t.Scope == ConquerorContextDataScope.AcrossTransports).Select(p => new KeyValuePair<string, string>(p.Key, (string)p.Value)));

        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    /// <summary>
    ///     Parse the given formatted data strings into key/value pairs.
    /// </summary>
    /// <param name="values">The values to parse</param>
    /// <returns>The parsed key/value pairs</returns>
    public static IReadOnlyCollection<(string Key, string Value)> Parse(IEnumerable<string> values)
    {
        try
        {
            return values.SelectMany(s => s.Split(';')).Select(s => s.Trim().Split(',')).Select(a => (Base64Decode(a[0]), Base64Decode(a[1]))).ToList();
        }
        catch (Exception e)
        {
            throw new FormattedConquerorContextDataInvalidException("an error occurred while parsing formatted context data", e);
        }
    }

    private static string Format(IEnumerable<KeyValuePair<string, string>> values)
    {
        return string.Join(";", values.Select(p => $"{Base64Encode(p.Key)},{Base64Encode(p.Value)}"));
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
