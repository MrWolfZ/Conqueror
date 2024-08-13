using System.Runtime.CompilerServices;

namespace Conqueror.Streaming.Tests;

internal static class AsyncEnumerableTestExtensions
{
    public static async Task<IReadOnlyCollection<TItem>> Drain<TItem>(this IAsyncEnumerable<TItem> enumerable)
    {
        var items = new List<TItem>();

        await foreach (var item in enumerable)
        {
            items.Add(item);
        }

        return items;
    }

    public static async Task<IReadOnlyCollection<TItem>> Drain<TItem>(this ConfiguredCancelableAsyncEnumerable<TItem> enumerable)
    {
        var items = new List<TItem>();

        await foreach (var item in enumerable)
        {
            items.Add(item);
        }

        return items;
    }
}
