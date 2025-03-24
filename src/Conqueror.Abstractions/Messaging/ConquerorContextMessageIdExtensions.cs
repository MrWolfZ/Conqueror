// ReSharper disable once CheckNamespace
namespace Conqueror;

public static class ConquerorContextMessageIdExtensions
{
    private const string MessageIdKey = "conqueror-message-id";

    /// <summary>
    ///     Get the ID of the currently executing message (if any).
    /// </summary>
    /// <param name="conquerorContext">the conqueror context to get the message ID from</param>
    /// <returns>the ID of the executing message if there is one, otherwise <c>null</c></returns>
    public static string? GetMessageId(this ConquerorContext conquerorContext)
    {
        return conquerorContext.DownstreamContextData.Get<string>(MessageIdKey);
    }

    /// <summary>
    ///     Set the ID of the currently executing message.
    /// </summary>
    /// <param name="conquerorContext">the conqueror context to set the message ID in</param>
    /// <param name="messageId">the message ID to set</param>
    public static void SetMessageId(this ConquerorContext conquerorContext, string messageId)
    {
        conquerorContext.DownstreamContextData.Set(MessageIdKey, messageId, ConquerorContextDataScope.AcrossTransports);
    }
}
