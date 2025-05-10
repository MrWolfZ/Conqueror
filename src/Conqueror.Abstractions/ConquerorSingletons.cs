using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Conqueror;

/// <summary>
///     This is a helper class which allows extension packages to register singletons at
///     runtime to allow for an API that does not require an explicit registration of
///     services.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class ConquerorSingletons(IServiceProvider serviceProvider) : IAsyncDisposable
{
    private readonly ConcurrentDictionary<Type, object> singletons = new();

    public T GetOrAddSingleton<T>(Func<IServiceProvider, T> factory)
        where T : class
        => (T)singletons.GetOrAdd(typeof(T), _ => factory(serviceProvider));

    public async ValueTask DisposeAsync()
    {
        foreach (var singleton in singletons.Values)
        {
            Debug.Assert(
                singleton is not (IAsyncDisposable and IDisposable),
                $"singleton of type {singleton.GetType()} is both {nameof(IAsyncDisposable)} and {nameof(IDisposable)}");

            if (singleton is IAsyncDisposable ad)
            {
                await ad.DisposeAsync().ConfigureAwait(false);
            }
            else if (singleton is IDisposable d)
            {
                d.Dispose();
            }
        }
    }
}
