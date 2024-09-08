namespace Conqueror.Common;

internal static class ConquerorContextCommonExtensions
{
    private static readonly string SignalExecutionFromTransportKey = typeof(ConquerorContextCommonExtensions).FullName + ".SignalExecutionFromTransport";

    /// <summary>
    ///     Signals to the next Conqueror operation execution that it was invoked from a transport.<br />
    ///     <br />
    ///     This method is typically called from a server-side transport implementation and does not need to be called by user-code.
    /// </summary>
    /// <param name="conquerorContext">The conqueror context to mark</param>
    /// <param name="transportTypeName">The name of the transport type that was executed</param>
    public static void SignalExecutionFromTransport(this IConquerorContext conquerorContext, string transportTypeName)
    {
        conquerorContext.DownstreamContextData.Set(SignalExecutionFromTransportKey, transportTypeName, ConquerorContextDataScope.InProcess);
    }

    public static string? DrainExecutionTransportTypeName(this IConquerorContext conquerorContext)
    {
        var value = conquerorContext.DownstreamContextData.Get<string>(SignalExecutionFromTransportKey);
        _ = conquerorContext.DownstreamContextData.Remove(SignalExecutionFromTransportKey);
        return value;
    }
}
