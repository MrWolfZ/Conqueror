using System;
using System.Collections.Generic;

namespace Conqueror
{
    /// <summary>
    ///     Encapsulates contextual information for conqueror executions (i.e. commands, queries, and events).
    /// </summary>
    public interface IConquerorContext
    {
        /// <summary>
        ///     Gets a key/value collection that can be used to share data across different executions (including
        ///     across transport boundaries, e.g. HTTP).
        /// </summary>
        IDictionary<string, string> Items { get; }

        /// <summary>
        ///     Returns true if there are any items in the context. Using this property is preferable to accessing
        ///     the count of <see cref="Items" /> for performance reasons, since the items may be lazily created.
        /// </summary>
        bool HasItems { get; }

        /// <summary>
        ///     Add items from <paramref name="source" /> to <see cref="Items" />.
        /// </summary>
        /// <param name="source">The items to add.</param>
        void AddOrReplaceItems(IEnumerable<KeyValuePair<string, string>> source)
        {
            foreach (var p in source)
            {
                Items[p.Key] = p.Value;
            }
        }

        /*
        /// <summary>
        ///     Gets or sets a unique identifier to represent this command in trace logs.
        /// </summary>
        string TraceIdentifier { get; set; }
        */
    }

    /// <inheritdoc cref="IConquerorContext" />
    public interface IDisposableConquerorContext : IConquerorContext, IDisposable
    {
    }
}
