using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Conqueror.Common;

internal sealed class DefaultConquerorContextData : IConquerorContextData
{
    private readonly Lazy<ConcurrentDictionary<string, (object Value, ConquerorContextDataScope Scope)>> itemsLazy = new();

    public IEnumerator<(string Key, object Value, ConquerorContextDataScope Scope)> GetEnumerator()
    {
        if (!itemsLazy.IsValueCreated)
        {
            yield break;
        }

        foreach (var (key, (value, scope)) in itemsLazy.Value)
        {
            yield return (key, value, scope);
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Set(string key, string value, ConquerorContextDataScope scope)
    {
        _ = itemsLazy.Value.AddOrUpdate(key, _ => (value, scope), (_, _) => (value, scope));
    }

    public void Set(string key, object value)
    {
        _ = itemsLazy.Value.AddOrUpdate(key, _ => (value, ConquerorContextDataScope.InProcess), (_, _) => (value, ConquerorContextDataScope.InProcess));
    }

    public bool Remove(string key)
    {
        if (!itemsLazy.IsValueCreated)
        {
            return false;
        }

        return itemsLazy.Value.TryRemove(key, out _);
    }

    public void Clear()
    {
        if (!itemsLazy.IsValueCreated)
        {
            return;
        }

        itemsLazy.Value.Clear();
    }

    public T? Get<T>(string key)
    {
        if (!itemsLazy.IsValueCreated)
        {
            return default;
        }

        var result = itemsLazy.Value.TryGetValue(key, out var v);

        if (!result)
        {
            return default;
        }

        return (T)v.Value;
    }
}
