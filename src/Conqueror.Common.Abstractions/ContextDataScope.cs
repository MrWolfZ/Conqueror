namespace Conqueror;

/// <summary>
///     The scope in which a piece of context data is valid.
/// </summary>
public enum ConquerorContextDataScope
{
    /// <summary>
    ///     The context data will be valid in the same process.<br />
    ///     <br />
    ///     For example, if command handler A executes query handler B in the same process, and query handler B
    ///     calls query handler C via a transport (e.g. via HTTP), then data set in command handler A will be
    ///     available in query handler B, but not in query handler C.
    /// </summary>
    InProcess = 1,

    /// <summary>
    ///     The context data will be valid in the same process and propagated across transports.<br />
    ///     <br />
    ///     For example, if command handler A executes query handler B in the same process, and query handler B
    ///     calls query handler C via a transport (e.g. via HTTP), then data set in command handler A will be
    ///     available in query handler B and in query handler C.
    /// </summary>
    AcrossTransports = 2,
}
