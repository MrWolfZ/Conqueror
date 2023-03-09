// ReSharper disable once CheckNamespace (we want these extensions to be accessible without an extra import)
namespace Conqueror;

public static class ConquerorContextCommonExtensions
{
    private static readonly string SignalExecutionFromTransportKey = typeof(ConquerorContextCommonExtensions).FullName + ".SignalExecutionFromTransport";

    /// <summary>
    ///     Signals to the next Conqueror operation execution that it was invoked from a transport.<br />
    ///     <br />
    ///     This method is typically called from a server-side transport implementation and does not need to be called by user-code.
    /// </summary>
    /// <param name="conquerorContext">The conqueror context to mark</param>
    public static void SignalExecutionFromTransport(this IConquerorContext conquerorContext)
    {
        conquerorContext.DownstreamContextData.Set(SignalExecutionFromTransportKey, string.Empty, ConquerorContextDataScope.InProcess);
    }

    internal static bool IsExecutionFromTransport(this IConquerorContext conquerorContext)
    {
        return conquerorContext.DownstreamContextData.Remove(SignalExecutionFromTransportKey);
    }
}
