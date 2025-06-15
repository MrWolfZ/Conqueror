namespace Conqueror.Transport.ConformityTests;

internal static class ConquerorContextDataExtensions
{
    public static IReadOnlyCollection<KeyValuePair<string, string>> AsKeyValuePairs(this IConquerorContextData contextData)
    {
        return contextData.AsKeyValuePairs<string>().ToList();
    }

    private static IEnumerable<KeyValuePair<string, T>> AsKeyValuePairs<T>(this IConquerorContextData contextData)
    {
        return contextData.Where(t => t.Value is T).Select(t => new KeyValuePair<string, T>(t.Key, (T)t.Value));
    }
}
