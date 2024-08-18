using System;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Conqueror;

public sealed class DefaultHttpStreamPathConvention : IHttpStreamPathConvention
{
    private static readonly Lazy<DefaultHttpStreamPathConvention> LazyInstance = new(() => new());
    private static readonly Regex StripSuffixRegex = new("(Stream(ing)?)?(Request)?$");
    private static readonly ConcurrentDictionary<Type, string> PathCache = new();

    private DefaultHttpStreamPathConvention()
    {
    }

    public static DefaultHttpStreamPathConvention Instance => LazyInstance.Value;

    public string GetStreamPath(Type requestType, HttpStreamAttribute attribute)
    {
        if (attribute.Path != null)
        {
            return attribute.Path;
        }

        // in clients this method may be called repeatedly, and since regex is expensive
        // we cache the result
        return PathCache.GetOrAdd(requestType, t =>
        {
            var versionPart = attribute.Version is null ? string.Empty : $"{attribute.Version}/";
            var namePart = StripSuffixRegex.Replace(t.Name, string.Empty);
            return $"/api/{versionPart}streams/{namePart}";
        });
    }
}
