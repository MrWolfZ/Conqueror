using System;
using System.Security.Claims;

// ReSharper disable once CheckNamespace
namespace Conqueror;

/// <summary>
///     Encapsulates contextual information for conqueror operations (i.e. messages, streaming requests, and signals).
/// </summary>
public abstract class ConquerorContext : IDisposable
{
    /// <summary>
    ///     The trace ID for the current Conqueror operation. If there is an active
    ///     <see cref="System.Diagnostics.Activity" />, the trace ID is taken from the
    ///     <see cref="System.Diagnostics.Activity.TraceId" /> property. Otherwise, this
    ///     is a randomly generated value.
    /// </summary>
    public abstract string TraceId { get; set; }

    /// <summary>
    ///     The ID of the currently executing message (if any).
    /// </summary>
    public abstract string? MessageId { get; set; }

    /// <summary>
    ///     The ID of the currently executing signal (if any).
    /// </summary>
    public abstract string? SignalId { get; set; }

    /// <summary>
    ///     The principal (if any) for the current Conqueror operation.
    /// </summary>
    public abstract ClaimsPrincipal? CurrentPrincipal { get; set; }

    /// <summary>
    ///     The data available in a Conqueror context which is transported across non-in-process transports.
    /// </summary>
    public abstract ITransportableConquerorContextData TransportableData { get; }

    /// <summary>
    ///     The in-process data available in a Conqueror context.
    /// </summary>
    public abstract IInProcessConquerorContextData InProcessData { get; }

    /// <summary>
    ///     Dispose the context. This will copy all upstream and bi-directional data to the context this context
    ///     was cloned from (if any).
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected abstract void Dispose(bool isDisposing);
}
