using System.Collections.Generic;

namespace Conqueror.Common
{
    internal static class CollectionExtensions
    {
        public static void SetRange<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, IEnumerable<KeyValuePair<TKey, TValue>> items)
        {
            foreach (var p in items)
            {
                dictionary[p.Key] = p.Value;
            }
        }
    }
}
