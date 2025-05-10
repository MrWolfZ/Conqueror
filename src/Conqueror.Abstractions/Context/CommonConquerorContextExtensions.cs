using System;
using System.Diagnostics;

// ReSharper disable once CheckNamespace
namespace Conqueror;

/// <summary>
///     Extension methods for Conqueror context that provide access to the trace ID.
/// </summary>
public static class CommonConquerorContextExtensions
{
    private const string TraceIdKey = "trace-id";

    /// <summary>
    ///     Initialize the trace ID for the current Conqueror operation. If there is an active
    ///     <see cref="System.Diagnostics.Activity" />, the trace ID is taken from the
    ///     <see cref="System.Diagnostics.Activity.TraceId" /> property. Otherwise, a new random
    ///     trace ID is generated. This method is typically called from a server-side transport
    ///     implementation and does not need to be called by user-code.
    /// </summary>
    /// <param name="conquerorContext">The Conqueror context to set the trace ID in</param>
    public static void InitializeTraceId(this ConquerorContext conquerorContext)
    {
        if (conquerorContext.DownstreamContextData.Get<string>(TraceIdKey) is null)
        {
            conquerorContext.SetTraceId(Activity.Current?.TraceId.ToString() ?? ActivityTraceId.CreateRandom().ToString());
        }
    }

    /// <summary>
    ///     Set the trace ID for the current Conqueror operation. This method is typically called from
    ///     a server-side transport implementation and does not need to be called by user-code.
    /// </summary>
    /// <param name="conquerorContext">The Conqueror context to set the trace ID in</param>
    /// <param name="traceId">The trace ID to set</param>
    public static void SetTraceId(this ConquerorContext conquerorContext, string traceId)
    {
        conquerorContext.DownstreamContextData.Set(TraceIdKey, traceId, ConquerorContextDataScope.AcrossTransports);
    }

    /// <summary>
    ///     Gets a unique identifier to represent all Conqueror operations in logs and traces.<br />
    ///     <br />
    ///     Note that if there is an active <see cref="System.Diagnostics.Activity" />, the trace ID
    ///     in the context will be the same as <see cref="System.Diagnostics.Activity.TraceId" />.
    /// </summary>
    /// <param name="conquerorContext">The Conqueror context to get the trace ID from</param>
    /// <returns>The trace ID of the current Conqueror operation</returns>
    public static string GetTraceId(this ConquerorContext conquerorContext)
    {
        return conquerorContext.DownstreamContextData.Get<string>(TraceIdKey) ?? throw new InvalidOperationException("trace ID was not set");
    }

    /// <summary>
    ///     Removes the trace ID from the context. This should only be used by transports
    /// to prevent sending the trace ID twice if it is already separately encoded (e.g.
    /// for HTTP in the traceparent header).
    /// </summary>
    /// <param name="conquerorContext">The Conqueror context to remove the trace ID from</param>
    /// <returns>The removed trace ID</returns>
    public static string RemoveTraceId(this ConquerorContext conquerorContext)
    {
        var traceId = conquerorContext.GetTraceId();

        if (!conquerorContext.DownstreamContextData.Remove(TraceIdKey))
        {
            throw new InvalidOperationException("trace ID was not set");
        }

        return traceId;
    }
}
