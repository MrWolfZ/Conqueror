namespace Conqueror;

/// <summary>
///     The direction in which context data flows.
/// </summary>
public enum ConquerorContextDataFlowDirection
{
    /// <summary>
    ///     The context data flows only to downstream Conqueror operations.<br />
    ///     <br />
    ///     For example, if command handler A executes query handler B, and query handler B calls query handler C,
    ///     then data set in command handler A will be available in query handler B and in query handler C, but data
    ///     set in query handler B is only available in query handler C, and not in command handler A.<br />
    ///     <br />
    ///     The data is also available to any code running as part of the current Conqueror operation, even if that
    ///     code is logically upstream. For example, downstream data set in a command handler is available to a
    ///     command middleware that is part of the handler's pipeline.
    /// </summary>
    Downstream = 0,

    /// <summary>
    ///     The context data flows only to upstream Conqueror operations.<br />
    ///     <br />
    ///     For example, if command handler A executes query handler B, and query handler B calls query handler C,
    ///     then data set in query handler C will be available in query handler B and in command handler A, but data
    ///     set in query handler B is only available in command handler A, and not in query handler C.<br />
    ///     <br />
    ///     The data is also available to any code running as part of the current Conqueror operation, even if that
    ///     code is logically downstream. For example, upstream data set in a command middleware is available to the
    ///     command handler.
    /// </summary>
    Upstream = 1,

    /// <summary>
    ///     The context data flows to up- and downstream Conqueror operations.<br />
    ///     <br />
    ///     For example, if command handler A executes query handler B, and query handler B calls query handler C,
    ///     then data set in query handler B will be available in query handler C and in command handler A. Data also
    ///     flows to siblings. For example, if command handler D first calls query handler E and then calls query
    ///     handler F, then data set in query handler E will be available in query handler F and command handler D.
    /// </summary>
    Bidirectional = 2,
}
