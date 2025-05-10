using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace Conqueror;

/// <summary>
///     This is a helper class which allows extension packages to register singletons at
///     runtime to allow for an API that does not require an explicit registration of
///     services.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class ConquerorSingletons(IServiceProvider serviceProvider) : IDisposable,
                                                                            IAsyncDisposable
{
    private readonly ConcurrentDictionary<Type, object> singletons = new();

    public T GetOrAddSingleton<T>(Func<IServiceProvider, T> factory)
        where T : class
        => (T)singletons.GetOrAdd(typeof(T), _ => factory(serviceProvider));

    public void Dispose()
    {
        foreach (var singleton in singletons.Values.OfType<IDisposable>())
        {
            singleton.Dispose();
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var singleton in singletons.Values.OfType<IAsyncDisposable>())
        {
            await singleton.DisposeAsync().ConfigureAwait(false);
        }
    }
}
