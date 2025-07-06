using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

// ReSharper disable once CheckNamespace
namespace Conqueror;

/// <summary>
///     A helper class to format and parse Conqueror context data as and from a string.
/// </summary>
public static class ConquerorContextDataFormattingExtensions
{
    /// <summary>
    ///     Encode all downstream and bidirectional context data as a string.
    /// </summary>
    /// <param name="ctx">The context to encode data from</param>
    /// <param name="traceId">The trace ID to encode</param>
    /// <param name="messageId">The message ID to encode</param>
    /// <param name="signalId">The signal ID to encode</param>
    /// <returns>The encoded data if any, otherwise <c>null</c></returns>
    public static string? EncodeDownstreamContextData(
        this ConquerorContext ctx,
        string? traceId = null,
        string? messageId = null,
        string? signalId = null)
    {
        var sb = new StringBuilder();

        EncodeIds(
            sb,
            traceId,
            messageId,
            signalId);

        ctx.TransportableData.GetAll(flowDirection: ConquerorContextDataFlowDirection.Downstream).Encode("d", sb);
        ctx.TransportableData.GetAll(flowDirection: ConquerorContextDataFlowDirection.Bidirectional).Encode("b", sb);

        return sb.Length > 0 ? sb.ToString() : null;
    }

    /// <summary>
    ///     Encode all upstream and bidirectional context data as a string.
    /// </summary>
    /// <param name="ctx">The context to encode data from</param>
    /// <returns>The encoded data if any, otherwise <c>null</c></returns>
    public static string? EncodeUpstreamContextData(this ConquerorContext ctx)
    {
        var sb = new StringBuilder();

        ctx.TransportableData.GetAll(flowDirection: ConquerorContextDataFlowDirection.Upstream).Encode("u", sb);
        ctx.TransportableData.GetAll(flowDirection: ConquerorContextDataFlowDirection.Bidirectional).Encode("b", sb);

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

    private static void EncodeIds(
        StringBuilder sb,
        string? traceId,
        string? messageId,
        string? signalId)
    {
        if (traceId is null && messageId is null && signalId is null)
        {
            return;
        }

        _ = sb.Append('c');

        if (traceId is not null)
        {
            _ = sb.Append("|trace-id:").Append(traceId);
        }

        if (messageId is not null)
        {
            _ = sb.Append("|message-id:").Append(messageId);
        }

        if (signalId is not null)
        {
            _ = sb.Append("|signal-id:").Append(signalId);
        }
    }

    private static void Encode(this IEnumerable<(string Key, string Value)> data, string type, StringBuilder sb)
    {
        var addedTypeTag = false;
        foreach (var (key, value) in data)
        {
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
            var index = 0;
            while (index < value.Length)
            {
                index = DecodeFromTypeTag(ctx, value, index);
            }
        }

        static int DecodeFromTypeTag(ConquerorContext ctx, string encodedData, int index)
        {
            var typeTag = encodedData[index];

            if (typeTag == 'c')
            {
                return DecodeWellKnownValues(ctx, encodedData, index + 2);
            }

            var flowDirection = typeTag switch
            {
                'b' => ConquerorContextDataFlowDirection.Bidirectional,
                'd' => ConquerorContextDataFlowDirection.Downstream,
                'u' => ConquerorContextDataFlowDirection.Upstream,
                _ => throw new InvalidOperationException($"unknown context data type tag '{typeTag}'"),
            };

            return DecodeType(
                ctx.TransportableData,
                encodedData,
                index + 2,
                flowDirection);
        }

        static int DecodeWellKnownValues(
            ConquerorContext ctx,
            string encodedData,
            int index)
        {
            while (index < encodedData.Length)
            {
                if (!DecodeKeyValue(
                        encodedData,
                        index,
                        out index,
                        out var key,
                        out var value))
                {
                    return index;
                }

                if (key is not null && value is not null)
                {
                    switch (key)
                    {
                        case "trace-id":
                            ctx.TraceId = value;

                            break;

                        case "message-id":
                            ctx.MessageId = value;

                            break;

                        case "signal-id":
                            ctx.SignalId = value;

                            break;

                        default:
                            throw new InvalidOperationException($"unknown well-known context data key '{key}'");
                    }
                }
            }

            return index;
        }

        static int DecodeType(
            ITransportableConquerorContextData ctxData,
            string encodedData,
            int index,
            ConquerorContextDataFlowDirection flowDirection)
        {
            while (index < encodedData.Length)
            {
                if (!DecodeKeyValue(
                        encodedData,
                        index,
                        out index,
                        out var key,
                        out var value))
                {
                    return index;
                }

                if (key is not null && value is not null)
                {
                    ctxData.Set(key, value, flowDirection);
                }
            }

            return index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool DecodeKeyValue(
            string encodedData,
            int index,
            out int nextIndex,
            out string? key,
            out string? value)
        {
            key = null;
            value = null;

            if (encodedData[index] == '|')
            {
                nextIndex = index + 1;

                return false;
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
            key = encodedData.Substring(index, keyLength);

            var valueLength = endIndex - separatorIndex;
            value = encodedData.Substring(separatorIndex + 1, valueLength);

            if (needsBase64Decoding)
            {
                key = Base64Decode(key);
                value = Base64Decode(value);
            }

            nextIndex = endIndex + 2;

            return true;
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
