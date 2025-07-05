using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Conqueror;

/// <summary>
///     The in-process data available in a Conqueror context. Note that there are different
///     "directions" in which the data can flow (see <see cref="ConquerorContextDataFlowDirection" />
///     for more details). Data for each direction is kept separately.
/// </summary>
public interface IInProcessConquerorContextData
{
    /// <summary>
    ///     Add a value for the key only if it does not exist yet (for the given flow direction).
    /// </summary>
    /// <param name="key">The key to set the value for</param>
    /// <param name="value">The value to set</param>
    /// <param name="flowDirection">The direction in which the data will flow</param>
    /// <returns><c>true</c> if the value was added, otherwise <c>false</c></returns>
    bool Add(string key, object value, ConquerorContextDataFlowDirection flowDirection = ConquerorContextDataFlowDirection.Downstream);

    /// <summary>
    ///     Set a value, overwriting any existing value with the same key (for the given flow direction).
    /// </summary>
    /// <param name="key">The key to set the value for</param>
    /// <param name="value">The value to set</param>
    /// <param name="flowDirection">The direction in which the data will flow</param>
    void Set(string key, object value, ConquerorContextDataFlowDirection flowDirection = ConquerorContextDataFlowDirection.Downstream);

    /// <summary>
    ///     Remove the value for the given key (for the given flow direction).
    /// </summary>
    /// <param name="key">The key to remove the value for</param>
    /// <param name="flowDirection">The flow direction for which to remove the value</param>
    /// <returns><c>true</c> if there was a value for the given key, otherwise <c>false</c></returns>
    bool Remove(string key, ConquerorContextDataFlowDirection flowDirection = ConquerorContextDataFlowDirection.Downstream);

    /// <summary>
    ///     Clear all context data (for the given flow direction).
    /// </summary>
    void Clear(ConquerorContextDataFlowDirection flowDirection = ConquerorContextDataFlowDirection.Downstream);

    /// <summary>
    ///     Get all context data (for the given flow direction).
    /// </summary>
    IEnumerable<(string Key, object Value)> GetAll(ConquerorContextDataFlowDirection flowDirection = ConquerorContextDataFlowDirection.Downstream);

    /// <summary>
    ///     Get the value for a key (for the given flow direction).
    /// </summary>
    /// <param name="key">The key to get the data for</param>
    /// <typeparam name="T">The expected type of the value</typeparam>
    /// <param name="flowDirection">The flow direction for which to get the value</param>
    /// <returns>The value if it exists, otherwise <c>null</c></returns>
    /// <exception cref="System.InvalidCastException">If the data for the given key is not of type <see cref="T" /></exception>
    T? Get<T>(string key, ConquerorContextDataFlowDirection flowDirection = ConquerorContextDataFlowDirection.Downstream);
}
