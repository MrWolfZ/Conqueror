using System;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Conqueror;

public sealed class DefaultHttpQueryPathConvention : IHttpQueryPathConvention
{
    private static readonly Lazy<DefaultHttpQueryPathConvention> LazyInstance = new(() => new());
    private static readonly Regex StripSuffixRegex = new("Query$");
    private static readonly ConcurrentDictionary<Type, string> PathCache = new();

    private DefaultHttpQueryPathConvention()
    {
    }

    public static DefaultHttpQueryPathConvention Instance => LazyInstance.Value;

    public string GetQueryPath(Type queryType, HttpQueryAttribute attribute)
    {
        if (attribute.Path != null)
        {
            return attribute.Path;
        }

        // in clients this method may be called repeatedly, and since regex is expensive
        // we cache the result
        return PathCache.GetOrAdd(queryType, t =>
        {
            var versionPart = attribute.Version is null ? string.Empty : $"{attribute.Version}/";
            var namePart = StripSuffixRegex.Replace(t.Name, string.Empty);
            return $"/api/{versionPart}queries/{namePart}";
        });
    }
}
