namespace Conqueror.Streaming.Interactive.Transport.Http.Client.Tests
{
    internal static class AsyncEnumerableTestExtensions
    {
        public static async Task<IReadOnlyCollection<TItem>> Drain<TItem>(this IAsyncEnumerable<TItem> enumerable, CancellationToken cancellationToken)
        {
            var items = new List<TItem>();
        
            await foreach (var item in enumerable.WithCancellation(cancellationToken))
            {
                items.Add(item);
            }

            return items;
        }
    }
}
