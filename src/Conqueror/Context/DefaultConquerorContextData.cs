using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

namespace Conqueror.Context;

// TODO: move to abstractions and replace interface with concrete class
public sealed class DefaultConquerorContextData : IConquerorContextData
{
    public static readonly DefaultConquerorContextData Empty = new();

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

    public void CopyTo(IConquerorContextData destination)
    {
        foreach (var (key, (value, scope)) in itemsLazy.Value)
        {
            if (value is string s)
            {
                destination.Set(key, s, scope);
                continue;
            }

            Debug.Assert(scope is ConquerorContextDataScope.InProcess, "only in-process scoped values cannot be strings");
            destination.Set(key, value);
        }
    }
}
