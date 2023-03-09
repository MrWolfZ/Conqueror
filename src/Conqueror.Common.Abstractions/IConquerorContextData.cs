using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Conqueror;

/// <summary>
///     The data available in a Conqueror context.
/// </summary>
public interface IConquerorContextData : IEnumerable<(string Key, object Value, ConquerorContextDataScope Scope)>
{
    /// <summary>
    ///     Set a value, overwriting any existing value with the same key.
    /// </summary>
    /// <param name="key">The key to set the value for</param>
    /// <param name="value">The value to set</param>
    /// <param name="scope">The scope in which the data is valid</param>
    void Set(string key, string value, ConquerorContextDataScope scope);

    /// <summary>
    ///     Set a value, overwriting any existing value with the same key.
    ///     The scope of the data is set to <see cref="ConquerorContextDataScope.InProcess" />.
    /// </summary>
    /// <param name="key">The key to set the value for</param>
    /// <param name="value">The value to set</param>
    void Set(string key, object value);

    /// <summary>
    ///     Remove the value for the given key.
    /// </summary>
    /// <param name="key">The key to remove the value for</param>
    /// <returns><c>true</c> if there was a value for the given key, otherwise <c>false</c></returns>
    bool Remove(string key);

    /// <summary>
    ///     Try to get the value for a key.
    /// </summary>
    /// <param name="key">The key to get the data for</param>
    /// <param name="value">The value for the key, if any</param>
    /// <typeparam name="T">The expected type of the value</typeparam>
    /// <returns><c>true</c> if a value for the given key exists, otherwise <c>false</c></returns>
    /// <exception cref="System.InvalidCastException">If the data for the given key is not of type <see cref="T"/></exception>
    bool TryGet<T>(string key, [NotNullWhen(true)] out T? value);
}
