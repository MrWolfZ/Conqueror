namespace Conqueror.Common.Transport.Http.Server.AspNetCore;

public static class ConquerorContextCommonExtensions
{
    private const string TransportTypeNameKey = "conqueror-transport-type-name";

    /// <summary>
    ///     Signals to the next Conqueror operation execution that it was invoked from a transport.<br />
    ///     <br />
    ///     This method is typically called from a server-side transport implementation and does not need to be called by user-code.
    /// </summary>
    /// <param name="conquerorContext">The conqueror context to mark</param>
    /// <param name="transportTypeName">The name of the transport type that was executed</param>
    public static void SignalExecutionFromTransport(this ConquerorContext conquerorContext, string transportTypeName)
    {
        conquerorContext.DownstreamContextData.Set(TransportTypeNameKey, transportTypeName, ConquerorContextDataScope.InProcess);
    }

    public static string? GetExecutionTransportTypeName(this ConquerorContext conquerorContext)
    {
        return conquerorContext.DownstreamContextData.Get<string>(TransportTypeNameKey);
    }
}
