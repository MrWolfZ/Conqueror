// ReSharper disable once CheckNamespace

namespace Conqueror;

public static class ConquerorContextSignalIdExtensions
{
    private const string SignalIdKey = "conqueror-signal-id";

    /// <summary>
    ///     Get the ID of the currently executing signal (if any).
    /// </summary>
    /// <param name="conquerorContext">the conqueror context to get the signal ID from</param>
    /// <returns>the ID of the executing signal if there is one, otherwise <c>null</c></returns>
    public static string? GetSignalId(this ConquerorContext conquerorContext)
    {
        return conquerorContext.DownstreamContextData.Get<string>(SignalIdKey);
    }

    /// <summary>
    ///     Set the ID of the currently executing signal.
    /// </summary>
    /// <param name="conquerorContext">the conqueror context to set the signal ID in</param>
    /// <param name="signalId">the signal ID to set</param>
    public static void SetSignalId(this ConquerorContext conquerorContext, string signalId)
    {
        conquerorContext.DownstreamContextData.Set(SignalIdKey, signalId, ConquerorContextDataScope.AcrossTransports);
    }

    /// <summary>
    ///     Remove the ID of the currently executing signal (if any) from the context. This should
    ///     only be used by transports to prevent sending the signal ID twice if it is already
    ///     separately encoded (e.g. for HTTP SSE in the event-id field).
    /// </summary>
    /// <param name="conquerorContext">the conqueror context to remove the signal ID from</param>
    /// <returns>the removed ID of the executing signal if there is one, otherwise <c>null</c></returns>
    public static string? RemoveSignalId(this ConquerorContext conquerorContext)
    {
        var signalId = conquerorContext.DownstreamContextData.Get<string>(SignalIdKey);
        _ = conquerorContext.DownstreamContextData.Remove(SignalIdKey);

        return signalId;
    }
}
