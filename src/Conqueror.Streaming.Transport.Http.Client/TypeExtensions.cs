using System;
using System.Reflection;

namespace Conqueror.Streaming.Transport.Http.Client;

internal static class TypeExtensions
{
    public static void AssertRequestIsHttpStream(this Type requestType)
    {
        var attribute = requestType.GetCustomAttribute<HttpStreamingRequestAttribute>();

        if (attribute == null)
        {
            throw new ArgumentException($"streaming request type '{requestType}' is not an HTTP request; did you forget to add '{nameof(HttpStreamingRequestAttribute)}'?");
        }
    }
}
