namespace Conqueror.Streaming.Tests;

internal static class AsyncEnumerableHelper
{
    public static async IAsyncEnumerable<TItem> Empty<TItem>()
    {
        await Task.Yield();
        yield break;
    }

    public static async IAsyncEnumerable<TItem> Of<TItem>(params TItem[] items)
    {
        await Task.Yield();

        foreach (var item in items)
        {
            yield return item;
        }
    }
}
