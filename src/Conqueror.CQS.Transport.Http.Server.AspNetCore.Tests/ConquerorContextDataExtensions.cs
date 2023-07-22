namespace Conqueror.CQS.Transport.Http.Server.AspNetCore.Tests;

internal static class ConquerorContextDataExtensions
{
    public static IEnumerable<KeyValuePair<string, T>> AsKeyValuePairs<T>(this IConquerorContextData contextData)
    {
        return contextData.Where(t => t.Value is T).Select(t => new KeyValuePair<string, T>(t.Key, (T)t.Value));
    }

    public static IEnumerable<KeyValuePair<string, string>> WhereScopeIsAcrossTransports(this IConquerorContextData contextData)
    {
        return contextData.Where(t => t.Scope == ConquerorContextDataScope.AcrossTransports).Select(t => new KeyValuePair<string, string>(t.Key, (string)t.Value));
    }
}
