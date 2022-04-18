using System;
using System.Reflection;

namespace Conqueror.CQS.Extensions.AspNetCore.Client
{
    internal static class TypeExtensions
    {
        public static void AssertCommandIsHttpCommand(this Type commandType)
        {
            var attribute = commandType.GetCustomAttribute<HttpCommandAttribute>();

            if (attribute == null)
            {
                throw new ArgumentException($"command type {commandType} is not an HTTP command; did you forget to add {nameof(HttpCommandAttribute)}?");
            }
        }

        public static void AssertQueryIsHttpQuery(this Type queryType)
        {
            var attribute = queryType.GetCustomAttribute<HttpQueryAttribute>();

            if (attribute == null)
            {
                throw new ArgumentException($"query type {queryType} is not an HTTP query; did you forget to add {nameof(HttpQueryAttribute)}?");
            }
        }
    }
}
