namespace Conqueror.CQS.Transport.Http.Server.AspNetCore.Tests;

internal static class CollectionExtensions
{
    public static void AddOrReplaceRange<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, IEnumerable<KeyValuePair<TKey, TValue>> items)
    {
        foreach (var p in items)
        {
            dictionary[p.Key] = p.Value;
        }
    }
}
