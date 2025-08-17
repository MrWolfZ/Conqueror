namespace Conqueror.Context;

using System.Collections;

internal sealed class DefaultInProcessConquerorContextData(DefaultInProcessConquerorContextData? parent = null)
    : IEnumerable<(string Key, object Value)>
{
    public static readonly DefaultInProcessConquerorContextData Empty = new();

    private ConcurrentDictionary<string, object>? items;

    // track keys that were removed in this context (to override parent values)
    private ConcurrentDictionary<string, object?>? removedKeys;

    public IEnumerator<(string Key, object Value)> GetEnumerator()
    {
        if (items is not null)
        {
            foreach (var (key, value) in items)
            {
                yield return (key, value);
            }
        }

        // yield entries from parent that haven't been overridden or removed
        if (parent is not null)
        {
            foreach (var (key, value) in parent)
            {
                if (items?.ContainsKey(key) is not true && removedKeys?.ContainsKey(key) is not true)
                {
                    yield return (key, value);
                }
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public bool Add(string key, object value)
    {
        var result = EnsureItems().GetOrAdd(key, value);
        _ = removedKeys?.TryRemove(key, out _);

        return result != value;
    }

    public void Set(string key, object value)
    {
        EnsureItems()[key] = value;
        _ = removedKeys?.TryRemove(key, out _);
    }

    public bool Remove(string key)
    {
        var removed = items?.TryRemove(key, out _) is true;

        _ = EnsureRemovedKeys().TryAdd(key, value: null);

        return removed || parent?.Get<object>(key) is not null;
    }

    public void Clear()
    {
        if (items is null)
        {
            return;
        }

        foreach (var key in items.Keys)
        {
            _ = EnsureRemovedKeys().TryAdd(key, value: null);
        }

        items.Clear();
    }

    public T? Get<T>(string key)
    {
        if (IsRemoved(key))
        {
            return default;
        }

        if (items is not null && items.TryGetValue(key, out var value))
        {
            return (T)value;
        }

        return parent is not null ? parent.Get<T>(key) : default;
    }

    public bool IsRemoved(string key) => removedKeys?.ContainsKey(key) is true;

    private ConcurrentDictionary<string, object> EnsureItems() =>
        LazyInitializer.EnsureInitialized(
            ref items,
            static () => new(concurrencyLevel: 1, capacity: 4, StringComparer.Ordinal)
        );

    private ConcurrentDictionary<string, object?> EnsureRemovedKeys() =>
        LazyInitializer.EnsureInitialized(
            ref removedKeys,
            static () => new(concurrencyLevel: 1, capacity: 4, StringComparer.Ordinal)
        );
}
