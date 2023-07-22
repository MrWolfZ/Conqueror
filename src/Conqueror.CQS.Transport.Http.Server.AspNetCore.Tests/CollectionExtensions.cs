namespace Conqueror.CQS.Transport.Http.Server.AspNetCore.Tests;

internal static class CollectionExtensions
{
    public static IEnumerable<KeyValuePair<TKey, TValue>> AsKeyValuePairs<TKey, TValue>(this IEnumerable<(TKey Key, TValue Value)> items)
    {
        return items.Select(t => new KeyValuePair<TKey, TValue>(t.Key, t.Value));
    }
}
