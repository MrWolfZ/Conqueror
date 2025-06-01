using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Conqueror.Context;

internal sealed class DefaultConquerorContextData(DefaultConquerorContextData? parent = null)
    : IConquerorContextData
{
    private ConcurrentDictionary<string, (object Value, ConquerorContextDataScope Scope)>? items;

    // track keys that were removed in this context (to override parent values)
    private ConcurrentDictionary<string, object?>? removedKeys;

    public bool IsEmpty => (items is null || items.IsEmpty) && (parent is null || parent.IsEmpty);

    public IEnumerator<(string Key, object Value, ConquerorContextDataScope Scope)> GetEnumerator()
    {
        if (items is not null)
        {
            foreach (var (key, (value, scope)) in items)
            {
                yield return (key, value, scope);
            }
        }

        // yield entries from parent that haven't been overridden or removed
        if (parent != null)
        {
            foreach (var (key, value, scope) in parent)
            {
                if ((items is null || !items.ContainsKey(key))
                    && (removedKeys is null || !removedKeys.ContainsKey(key)))
                {
                    yield return (key, value, scope);
                }
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Set(string key, string value, ConquerorContextDataScope scope)
    {
        EnsureItems()[key] = (value, scope);
        _ = removedKeys is not null && removedKeys.TryRemove(key, out _);
    }

    public void Set(string key, object value)
    {
        EnsureItems()[key] = (value, ConquerorContextDataScope.InProcess);
        _ = removedKeys is not null && removedKeys.TryRemove(key, out _);
    }

    public bool Remove(string key)
    {
        var removed = items is not null && items.TryRemove(key, out _);

        _ = EnsureRemovedKeys().TryAdd(key, null);

        return removed || parent?.Get<object>(key) != null;
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

    public T? Get<T>(string key)
    {
        if (IsRemoved(key))
        {
            return default;
        }

        if (items is not null && items.TryGetValue(key, out var value))
        {
            return (T)value.Value;
        }

        return parent is not null ? parent.Get<T>(key) : default;
    }

    public bool IsRemoved(string key) => removedKeys is not null && removedKeys.ContainsKey(key);

    private ConcurrentDictionary<string, (object Value, ConquerorContextDataScope Scope)> EnsureItems()
        => LazyInitializer.EnsureInitialized(ref items, static () => new(1, 4));

    private ConcurrentDictionary<string, object?> EnsureRemovedKeys()
        => LazyInitializer.EnsureInitialized(ref removedKeys, static () => new(1, 4));
}
