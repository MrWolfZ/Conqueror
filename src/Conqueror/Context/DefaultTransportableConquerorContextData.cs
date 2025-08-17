namespace Conqueror.Context;

using System.Collections;

internal sealed class DefaultTransportableConquerorContextData(DefaultTransportableConquerorContextData? parent = null)
    : IEnumerable<(string Key, string Value)>
{
    public static readonly DefaultTransportableConquerorContextData Empty = new();

    private ConcurrentDictionary<string, string>? items;

    // track keys that were removed in this context (to override parent values)
    private ConcurrentDictionary<string, string?>? removedKeys;

    public IEnumerator<(string Key, string Value)> GetEnumerator()
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
                if (((items?.ContainsKey(key)) is not true) && ((removedKeys?.ContainsKey(key)) is not true))
                {
                    yield return (key, value);
                }
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public bool Add(string key, string value)
    {
        var result = EnsureItems().GetOrAdd(key, value);
        _ = removedKeys?.TryRemove(key, out _);

        return !string.Equals(result, value, StringComparison.Ordinal);
    }

    public void Set(string key, string value)
    {
        EnsureItems()[key] = value;
        _ = removedKeys?.TryRemove(key, out _);
    }

    public bool Remove(string key)
    {
        var removed = (items?.TryRemove(key, out _)) is true;

        _ = EnsureRemovedKeys().TryAdd(key, value: null);

        return removed || parent?.Get(key) is not null;
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

    public string? Get(string key)
    {
        if (IsRemoved(key))
        {
            return null;
        }

        if (items is not null && items.TryGetValue(key, out var value))
        {
            return value;
        }

        return parent?.Get(key);
    }

    public bool IsRemoved(string key) => removedKeys?.ContainsKey(key) ?? false;

    private ConcurrentDictionary<string, string> EnsureItems() =>
        LazyInitializer.EnsureInitialized(
            ref items,
            static () => new(concurrencyLevel: 1, capacity: 4, StringComparer.Ordinal)
        );

    private ConcurrentDictionary<string, string?> EnsureRemovedKeys() =>
        LazyInitializer.EnsureInitialized(
            ref removedKeys,
            static () => new(concurrencyLevel: 1, capacity: 4, StringComparer.Ordinal)
        );
}
