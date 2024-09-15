// ReSharper disable once CheckNamespace (we want these extensions to be accessible without an extra import)

namespace Conqueror;

public static class ConquerorContextStreamingExtensions
{
    private const string StreamingRequestIdKey = "conqueror-streaming-request-id";

    /// <summary>
    ///     Get the ID of the currently executing streaming request (if any).
    /// </summary>
    /// <param name="conquerorContext">the conqueror context to get the streaming request ID from</param>
    /// <returns>the ID of the executing streaming request if there is one, otherwise <c>null</c></returns>
    public static string? GetStreamingRequestId(this ConquerorContext conquerorContext)
    {
        return conquerorContext.DownstreamContextData.Get<string>(StreamingRequestIdKey);
    }

    /// <summary>
    ///     Set the ID of the currently executing streaming request.
    /// </summary>
    /// <param name="conquerorContext">the conqueror context to set the streaming request ID in</param>
    /// <param name="streamingRequestId">the streaming request ID to set</param>
    public static void SetStreamingRequestId(this ConquerorContext conquerorContext, string streamingRequestId)
    {
        conquerorContext.DownstreamContextData.Set(StreamingRequestIdKey, streamingRequestId, ConquerorContextDataScope.AcrossTransports);
    }
}
