namespace Conqueror.Streaming.Tests;

using System.Runtime.CompilerServices;

internal static class AsyncEnumerableTestExtensions
{
    public static async Task<IReadOnlyCollection<TItem>> Drain<TItem>(
        this IAsyncEnumerable<TItem> enumerable,
        CancellationToken cancellationToken = default
    )
    {
        var items = new List<TItem>();

        await foreach (var item in enumerable.WithCancellation(cancellationToken))
        {
            items.Add(item);
        }

        return items;
    }

    public static async Task<IReadOnlyCollection<TItem>> Drain<TItem>(
        this ConfiguredCancelableAsyncEnumerable<TItem> enumerable,
        CancellationToken cancellationToken = default
    )
    {
        var items = new List<TItem>();

        await foreach (var item in enumerable.WithCancellation(cancellationToken))
        {
            items.Add(item);
        }

        return items;
    }
}
