namespace Conqueror.Streaming.Transport.Http.Client.Tests;

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
}
