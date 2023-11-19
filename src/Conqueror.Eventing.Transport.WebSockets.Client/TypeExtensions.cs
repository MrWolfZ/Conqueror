using System;
using System.Reflection;

namespace Conqueror.Eventing.Transport.WebSockets.Client;

internal static class TypeExtensions
{
    public static void AssertEventIsWebSocketsEvent(this Type eventType)
    {
        var attribute = eventType.GetCustomAttribute<WebSocketsEventAttribute>();

        if (attribute == null)
        {
            throw new ArgumentException($"event type {eventType} is not a websockets event; did you forget to add {nameof(WebSocketsEventAttribute)}?");
        }
    }
}
