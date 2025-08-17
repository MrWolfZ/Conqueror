namespace Conqueror;

/// <summary>
///     The data available in a Conqueror context which is transported across non-in-process transports.
///     Note that there are different "directions" in which the data can flow (see
///     <see cref="ConquerorContextDataFlowDirection" /> for more details). Data for each direction is
///     kept separately.
/// </summary>
public interface ITransportableConquerorContextData
{
    /// <summary>
    ///     Add a value for the key only if it does not exist yet (for the given flow direction).
    /// </summary>
    /// <param name="key">The key to set the value for</param>
    /// <param name="value">The value to set</param>
    /// <param name="flowDirection">The direction in which the data will flow</param>
    /// <returns><see langword="true" /> if the value was added, otherwise <see langword="false" /></returns>
    bool Add(
        string key,
        string value,
        ConquerorContextDataFlowDirection flowDirection = ConquerorContextDataFlowDirection.Downstream
    );

    /// <summary>
    ///     Set a value, overwriting any existing value with the same key (for the given flow direction).
    /// </summary>
    /// <param name="key">The key to set the value for</param>
    /// <param name="value">The value to set</param>
    /// <param name="flowDirection">The direction in which the data will flow</param>
    void Set(
        string key,
        string value,
        ConquerorContextDataFlowDirection flowDirection = ConquerorContextDataFlowDirection.Downstream
    );

    /// <summary>
    ///     Remove the value for the given key (for the given flow direction).
    /// </summary>
    /// <param name="key">The key to remove the value for</param>
    /// <param name="flowDirection">The flow direction for which to remove the value</param>
    /// <returns><see langword="true" /> if there was a value for the given key, otherwise <see langword="false" /></returns>
    bool Remove(
        string key,
        ConquerorContextDataFlowDirection flowDirection = ConquerorContextDataFlowDirection.Downstream
    );

    /// <summary>
    ///     Clear all context data (for the given flow direction).
    /// </summary>
    /// <param name="flowDirection">The flow direction for which to clear all values</param>
    void Clear(ConquerorContextDataFlowDirection flowDirection = ConquerorContextDataFlowDirection.Downstream);

    /// <summary>
    ///     Get all context data (for the given flow direction).
    /// </summary>
    /// <param name="flowDirection">The flow direction for which to get all values</param>
    /// <returns>All values for the given flow direction</returns>
    IEnumerable<(string Key, string Value)> GetAll(
        ConquerorContextDataFlowDirection flowDirection = ConquerorContextDataFlowDirection.Downstream
    );

    /// <summary>
    ///     Get the value for a key (for the given flow direction).
    /// </summary>
    /// <param name="key">The key to get the data for</param>
    /// <param name="flowDirection">The flow direction for which to get the value</param>
    /// <returns>The value if it exists, otherwise <see langword="null" /></returns>
    string? Get(
        string key,
        ConquerorContextDataFlowDirection flowDirection = ConquerorContextDataFlowDirection.Downstream
    );
}
