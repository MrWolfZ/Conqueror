// ReSharper disable once CheckNamespace
namespace Conqueror;

public static class ConquerorContextEventNotificationIdExtensions
{
    private const string EventNotificationIdKey = "conqueror-event-notification-id";

    /// <summary>
    ///     Get the ID of the currently executing event notification (if any).
    /// </summary>
    /// <param name="conquerorContext">the conqueror context to get the event notification ID from</param>
    /// <returns>the ID of the executing event notification if there is one, otherwise <c>null</c></returns>
    public static string? GetEventNotificationId(this ConquerorContext conquerorContext)
    {
        return conquerorContext.DownstreamContextData.Get<string>(EventNotificationIdKey);
    }

    /// <summary>
    ///     Set the ID of the currently executing event notification.
    /// </summary>
    /// <param name="conquerorContext">the conqueror context to set the event notification ID in</param>
    /// <param name="notificationId">the event notification ID to set</param>
    public static void SetEventNotificationId(this ConquerorContext conquerorContext, string notificationId)
    {
        conquerorContext.DownstreamContextData.Set(EventNotificationIdKey, notificationId, ConquerorContextDataScope.AcrossTransports);
    }
}
