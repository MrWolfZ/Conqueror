using System;
using System.Reflection;

namespace Conqueror.Streaming.Interactive.Transport.Http.Client
{
    internal static class TypeExtensions
    {
        public static void AssertRequestIsHttpInteractiveStream(this Type requestType)
        {
            var attribute = requestType.GetCustomAttribute<HttpInteractiveStreamingRequestAttribute>();

            if (attribute == null)
            {
                throw new ArgumentException($"interactive streaming request type '{requestType}' is not an HTTP query; did you forget to add '{nameof(HttpInteractiveStreamingRequestAttribute)}'?");
            }
        }
    }
}
