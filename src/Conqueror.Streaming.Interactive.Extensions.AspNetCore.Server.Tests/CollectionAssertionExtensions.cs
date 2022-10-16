using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;

namespace Conqueror.Streaming.Interactive.Extensions.AspNetCore.Server.Tests;

public static class CollectionAssertionExtensions
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new();

    public static void ShouldReceiveItem<T>(this BlockingCollection<T> collection, T item, TimeSpan? timeout = null)
        where T : notnull
    {
        var result = collection.TryTake(out var receivedItem, Debugger.IsAttached ? TimeSpan.FromMinutes(1) : timeout ?? TimeSpan.FromMilliseconds(500));

        if (!result)
        {
            Assert.Fail($"did not receive item in time: {item}");
        }

        Assert.NotNull(receivedItem);
        Assert.AreEqual(Serialize(receivedItem!), Serialize(item));
    }

    public static void ShouldNotReceiveAnyItem<T>(this BlockingCollection<T> collection, TimeSpan? waitFor = null)
        where T : notnull
    {
        T? item;

        var result = waitFor != null ? collection.TryTake(out item, Debugger.IsAttached ? TimeSpan.FromMinutes(1) : waitFor.Value) : collection.TryTake(out item);

        if (result)
        {
            Assert.Fail($"received unexpected item '{item}'");
        }
    }

    private static string Serialize(object o) => o is string s ? s : JsonSerializer.Serialize(o, JsonSerializerOptions);
}
