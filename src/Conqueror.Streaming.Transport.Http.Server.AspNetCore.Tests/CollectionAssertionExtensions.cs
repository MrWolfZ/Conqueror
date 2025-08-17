namespace Conqueror.Streaming.Transport.Http.Server.AspNetCore.Tests;

using System.Collections.Concurrent;
using System.Diagnostics;

public static class CollectionAssertionExtensions
{
    public static void ShouldReceiveItem<T>(this BlockingCollection<T> collection, T item)
        where T : notnull
    {
        var result = collection.TryTake(
            out var receivedItem,
            Debugger.IsAttached ? TimeSpan.FromMinutes(value: 1)
                : Environment.GetEnvironmentVariable("GITHUB_ACTION") is not null ? TimeSpan.FromSeconds(value: 10)
                : TimeSpan.FromSeconds(value: 2)
        );

        if (!result)
        {
            Assert.Fail($"did not receive item in time: {item}");
        }

        Assert.That(receivedItem, Is.Not.Null);
        Assert.That(receivedItem, Is.EqualTo(item));
    }

    public static void ShouldNotReceiveAnyItem<T>(this BlockingCollection<T> collection, TimeSpan? waitFor = null)
        where T : notnull
    {
        var result = waitFor is not null
            ? collection.TryTake(out var item, Debugger.IsAttached ? TimeSpan.FromMinutes(value: 1) : waitFor.Value)
            : collection.TryTake(out item);

        if (result)
        {
            Assert.Fail($"received unexpected item '{item}'");
        }
    }
}
