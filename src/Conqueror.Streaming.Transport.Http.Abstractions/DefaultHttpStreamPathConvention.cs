namespace Conqueror;

using System.Collections.Concurrent;
using System.Text.RegularExpressions;

public sealed partial class DefaultHttpStreamPathConvention : IHttpStreamPathConvention
{
    private static readonly Lazy<DefaultHttpStreamPathConvention> LazyInstance = new(() => new());
    private static readonly ConcurrentDictionary<Type, string> PathCache = [];

    private DefaultHttpStreamPathConvention() { }

    public static DefaultHttpStreamPathConvention Instance => LazyInstance.Value;

    public string GetStreamPath(Type requestType, HttpStreamAttribute attribute)
    {
        if (attribute.Path is not null)
        {
            return attribute.Path;
        }

        // in clients this method may be called repeatedly, and since regex is expensive
        // we cache the result
        return PathCache.GetOrAdd(
            requestType,
            static (t, attribute) =>
            {
                var versionPart = attribute.Version is null ? "" : $"{attribute.Version}/";
                var namePart = StripSuffixRegex().Replace(t.Name, "");

                return $"/api/{versionPart}streams/{namePart}";
            },
            attribute
        );
    }

    [GeneratedRegex(
        "(Stream(ing)?)?(Request)?$",
        RegexOptions.Compiled | RegexOptions.ExplicitCapture,
        matchTimeoutMilliseconds: 2000
    )]
    private static partial Regex StripSuffixRegex();
}
