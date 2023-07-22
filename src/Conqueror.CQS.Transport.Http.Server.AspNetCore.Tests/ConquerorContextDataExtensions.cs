namespace Conqueror.CQS.Transport.Http.Server.AspNetCore.Tests;

internal static class ConquerorContextDataExtensions
{
    public static IEnumerable<KeyValuePair<string, T>> AsKeyValuePairs<T>(this IConquerorContextData contextData)
    {
        return contextData.Where(t => t.Value is T).Select(t => new KeyValuePair<string, T>(t.Key, (T)t.Value));
    }
}
