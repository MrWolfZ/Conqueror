namespace Conqueror.Streaming.Transport.Http.Server.AspNetCore.Tests;

internal static class ConquerorContextDataExtensions
{
    public static IEnumerable<KeyValuePair<string, string>> WhereScopeIsAcrossTransports(this IConquerorContextData contextData)
    {
        return contextData.Where(t => t.Scope == ConquerorContextDataScope.AcrossTransports).Select(t => new KeyValuePair<string, string>(t.Key, (string)t.Value));
    }
}
