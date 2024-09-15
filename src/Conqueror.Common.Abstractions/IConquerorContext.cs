using System;

namespace Conqueror;

/// <summary>
///     Encapsulates contextual information for conqueror executions (i.e. commands, queries, and events).
/// </summary>
public interface IConquerorContext
{
    /// <summary>
    ///     The context data which flows only to downstream Conqueror operations.<br />
    ///     <br />
    ///     For example, if command handler A executes query handler B, and query handler B calls query handler C,
    ///     then data set in command handler A will be available in query handler B and in query handler C, but data
    ///     set in query handler B is only available in query handler C, and not in command handler A. Note that the
    ///     validity of the data is also affected by its scope in addition to its direction (see
    ///     <see cref="ConquerorContextDataScope" />).<br />
    ///     <br />
    ///     The data is also available to any code running as part of the current Conqueror operation, even if that
    ///     code is logically upstream. For example, downstream data set in a command handler is available to a
    ///     command middleware that is part of the handler's pipeline.
    /// </summary>
    IConquerorContextData DownstreamContextData { get; }

    /// <summary>
    ///     The context data which flows only to upstream Conqueror operations.<br />
    ///     <br />
    ///     For example, if command handler A executes query handler B, and query handler B calls query handler C,
    ///     then data set in query handler C will be available in query handler B and in command handler A, but data
    ///     set in query handler B is only available in command handler A, and not in query handler C. Note that the
    ///     validity of the data is also affected by its scope in addition to its direction (see
    ///     <see cref="ConquerorContextDataScope" />).<br />
    ///     <br />
    ///     The data is also available to any code running as part of the current Conqueror operation, even if that
    ///     code is logically downstream. For example, upstream data set in a command middleware is available to the
    ///     command handler.
    /// </summary>
    IConquerorContextData UpstreamContextData { get; }

    /// <summary>
    ///     The context data which flows to up- and downstream Conqueror operations.<br />
    ///     <br />
    ///     For example, if command handler A executes query handler B, and query handler B calls query handler C,
    ///     then data set in query handler B will be available in query handler C and in command handler A. Data also
    ///     flows to siblings. For example, if command handler D first calls query handler E and then calls query
    ///     handler F, then data set in query handler E will be available in query handler F and command handler D.
    ///     Note that the validity of the data is also affected by its scope (see
    ///     <see cref="ConquerorContextDataScope" />).<br />
    /// </summary>
    IConquerorContextData ContextData { get; }
}

/// <inheritdoc cref="IConquerorContext" />
public interface IDisposableConquerorContext : IConquerorContext, IDisposable;
