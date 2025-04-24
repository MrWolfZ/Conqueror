using System.Collections.Generic;

// ReSharper disable once CheckNamespace
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
    ///     Clear all context data.
    /// </summary>
    void Clear();

    /// <summary>
    ///     Get the value for a key.
    /// </summary>
    /// <param name="key">The key to get the data for</param>
    /// <typeparam name="T">The expected type of the value</typeparam>
    /// <returns>The value if it exists, otherwise <c>null</c></returns>
    /// <exception cref="System.InvalidCastException">If the data for the given key is not of type <see cref="T"/></exception>
    T? Get<T>(string key);

    /// <summary>
    ///     Copy all context data from this instance to the destination instance.
    /// </summary>
    /// <param name="destination">the context data instance to copy the data to</param>
    void CopyTo(IConquerorContextData destination);
}
