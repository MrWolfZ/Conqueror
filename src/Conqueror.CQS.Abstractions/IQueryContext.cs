using System.Collections.Generic;

namespace Conqueror;

/// <summary>
///     Encapsulates contextual information for a query execution.
/// </summary>
public interface IQueryContext
{
    /// <summary>
    ///     Gets the query object.
    /// </summary>
    object Query { get; }

    /// <summary>
    ///     Gets the response object (if it is already set).
    /// </summary>
    object? Response { get; }

    /// <summary>
    ///     The unique ID of this query (useful e.g. for correlating log entries).
    /// </summary>
    string QueryId { get; }

    /// <summary>
    ///     Gets a key/value collection that can be used to share data within the scope of this query.
    /// </summary>
    IDictionary<object, object?> Items { get; }

    /// <summary>
    ///     Add items from <paramref name="source" /> to <see cref="Items" />.
    /// </summary>
    /// <param name="source">The items to add.</param>
    void AddOrReplaceItems(IEnumerable<KeyValuePair<object, object?>> source)
    {
        foreach (var p in source)
        {
            Items[p.Key] = p.Value;
        }
    }

    /*
    /// <summary>
    ///     Notifies when the query is aborted.
    /// </summary>
    CancellationToken CommandAborted { get; set; }

    /// <summary>
    ///     Gets or sets a unique identifier to represent this query in trace logs.
    /// </summary>
    string TraceIdentifier { get; set; }

    /// <summary>
    ///     Aborts the query.
    /// </summary>
    void Abort();
    */
}
