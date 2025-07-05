using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Conqueror.Context;

internal sealed class DefaultTransportableConquerorContextData(DefaultTransportableConquerorContextData? parent = null)
    : IEnumerable<(string Key, string Value)>
{
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
        if (parent != null)
        {
            foreach (var (key, value) in parent)
            {
                if ((items is null || !items.ContainsKey(key))
                    && (removedKeys is null || !removedKeys.ContainsKey(key)))
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
        _ = removedKeys is not null && removedKeys.TryRemove(key, out _);

        return result != value;
    }

    public void Set(string key, string value)
    {
        EnsureItems()[key] = value;
        _ = removedKeys is not null && removedKeys.TryRemove(key, out _);
    }

    public bool Remove(string key)
    {
        var removed = items is not null && items.TryRemove(key, out _);

        _ = EnsureRemovedKeys().TryAdd(key, null);

        return removed || parent?.Get(key) != null;
    }

    public void Clear()
    {
        if (items is null)
        {
            return;
        }

        foreach (var key in items.Keys)
        {
            _ = EnsureRemovedKeys().TryAdd(key, null);
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

    public bool IsRemoved(string key) => removedKeys is not null && removedKeys.ContainsKey(key);

    private ConcurrentDictionary<string, string> EnsureItems()
        => LazyInitializer.EnsureInitialized(ref items, static () => new(1, 4));

    private ConcurrentDictionary<string, string?> EnsureRemovedKeys()
        => LazyInitializer.EnsureInitialized(ref removedKeys, static () => new(1, 4));
}
