using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// ReSharper disable once CheckNamespace
namespace Conqueror;

/// <summary>
///     A helper class to format and parse Conqueror context data as and from a string.
/// </summary>
public static class ConquerorContextDataFormattingExtensions
{
    /// <summary>
    ///     Encode all downstream and bi-directional context data with scope <see cref="ConquerorContextDataScope.AcrossTransports" /> as a string.
    /// </summary>
    /// <param name="ctx">The context to encode data from</param>
    /// <returns>The encoded data if any, otherwise <c>null</c></returns>
    public static string? EncodeDownstreamContextData(this ConquerorContext ctx)
    {
        var sb = new StringBuilder();
        ctx.DownstreamContextData.Encode("d", sb);
        ctx.ContextData.Encode("b", sb);
        return sb.Length > 0 ? sb.ToString() : null;
    }

    /// <summary>
    ///     Encode all upstream and bi-directional context data with scope <see cref="ConquerorContextDataScope.AcrossTransports" /> as a string.
    /// </summary>
    /// <param name="ctx">The context to encode data from</param>
    /// <returns>The encoded data if any, otherwise <c>null</c></returns>
    public static string? EncodeUpstreamContextData(this ConquerorContext ctx)
    {
        var sb = new StringBuilder();
        ctx.UpstreamContextData.Encode("u", sb);
        ctx.ContextData.Encode("b", sb);
        return sb.Length > 0 ? sb.ToString() : null;
    }

    /// <summary>
    ///     Decode the given encoded data strings into key/value pairs.
    /// </summary>
    /// <param name="ctx">The context to decode data into</param>
    /// <param name="values">The encoded values to decode</param>
    public static void DecodeContextData(this ConquerorContext ctx, string values)
    {
        ctx.DecodeContextData([values]);
    }

    /// <summary>
    ///     Decode the given encoded data strings into key/value pairs.
    /// </summary>
    /// <param name="ctx">The context to decode data into</param>
    /// <param name="values">The encoded values to decode</param>
    public static void DecodeContextData(this ConquerorContext ctx, IEnumerable<string> values)
    {
        try
        {
            ctx.Decode(values);
        }
        catch (Exception e)
        {
            throw new FormattedConquerorContextDataInvalidException("an error occurred while parsing formatted Conqueror context data", e);
        }
    }

    private static void Encode(this IConquerorContextData? data, string type, StringBuilder sb)
    {
        if (data is null)
        {
            return;
        }

        var addedTypeTag = false;
        foreach (var (key, valueObj, _) in data.Where(t => t.Scope == ConquerorContextDataScope.AcrossTransports))
        {
            var value = (string)valueObj;

            if (!addedTypeTag)
            {
                if (sb.Length > 0)
                {
                    _ = sb.Append("||");
                }

                _ = sb.Append(type);
                addedTypeTag = true;
            }

            _ = sb.Append('|');

            if (key.Contains('|') || key.Contains(':') || value.Contains('|') || value.Contains(':'))
            {
                // since the key or value include our delimiter characters, we need to base64 encode it
                _ = sb.Append(':').Append(Base64Encode(key)).Append(':').Append(Base64Encode(value));
            }
            else
            {
                _ = sb.Append(key).Append(':').Append(value);
            }
        }
    }

    private static void Decode(this ConquerorContext ctx, IEnumerable<string> values)
    {
        foreach (var value in values)
        {
            int index = 0;
            while (index < value.Length)
            {
                index = DecodeFromTypeTag(ctx, value, index);
            }
        }

        static int DecodeFromTypeTag(ConquerorContext ctx, string encodedData, int index)
        {
            var typeTag = encodedData[index];
            var ctxData = typeTag switch
            {
                'b' => ctx.ContextData,
                'd' => ctx.DownstreamContextData,
                'u' => ctx.UpstreamContextData,
                _ => throw new InvalidOperationException($"unknown context data type tag '{typeTag}'"),
            };

            return DecodeType(ctxData, encodedData, index + 2);
        }

        static int DecodeType(IConquerorContextData ctxData, string encodedData, int index)
        {
            while (index < encodedData.Length)
            {
                if (encodedData[index] == '|')
                {
                    return index + 1;
                }

                var needsBase64Decoding = false;
                if (encodedData[index] == ':')
                {
                    index += 1;
                    needsBase64Decoding = true;
                }

                var endIndex = encodedData.IndexOf('|', index);
                endIndex = endIndex > 0 ? endIndex - 1 : encodedData.Length - 1;
                var separatorIndex = encodedData.IndexOf(':', index);

                var keyLength = separatorIndex - index;
                var key = encodedData.Substring(index, keyLength);

                var valueLength = endIndex - separatorIndex;
                var value = encodedData.Substring(separatorIndex + 1, valueLength);

                if (needsBase64Decoding)
                {
                    key = Base64Decode(key);
                    value = Base64Decode(value);
                }

                ctxData.Set(key, value, ConquerorContextDataScope.AcrossTransports);

                index = endIndex + 2;
            }

            return index;
        }
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
