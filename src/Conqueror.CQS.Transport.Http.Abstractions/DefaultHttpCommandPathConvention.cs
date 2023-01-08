using System;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Conqueror
{
    public sealed class DefaultHttpCommandPathConvention : IHttpCommandPathConvention
    {
        private static readonly Regex StripSuffixRegex = new("Command$");
        private static readonly ConcurrentDictionary<Type, string> PathCache = new();

        public string GetCommandPath(Type commandType, HttpCommandAttribute attribute)
        {
            if (attribute.Path != null)
            {
                return attribute.Path;
            }

            // in clients this method may be called repeatedly, and since regex is expensive
            // we cache the result
            return PathCache.GetOrAdd(commandType, t =>
            {
                var versionPart = attribute.Version is null ? string.Empty : $"{attribute.Version}/";
                var namePart = StripSuffixRegex.Replace(t.Name, string.Empty);
                return $"/api/{versionPart}commands/{namePart}";
            });
        }
    }
}
