using System.Collections.Generic;

namespace Conqueror.CQS
{
    /// <summary>
    ///     Encapsulates contextual information for a command execution.
    /// </summary>
    public interface ICommandContext
    {
        /// <summary>
        ///     Gets the command object.
        /// </summary>
        object Command { get; }

        /// <summary>
        ///     Gets the response object (if it is already set).
        /// </summary>
        object? Response { get; }

        /// <summary>
        ///     Gets a key/value collection that can be used to share data within the scope of this command.
        /// </summary>
        IDictionary<object, object?> Items { get; }

        /// <summary>
        ///     Gets a key/value collection that can be used to share data across different commands or transport boundaries.
        /// </summary>
        IDictionary<string, string> TransferrableItems { get; }

        /// <summary>
        ///     Add items from <paramref name="source"/> to <see cref="Items" />.
        /// </summary>
        /// <param name="source">The items to add.</param>
        void AddItems(IEnumerable<KeyValuePair<object, object?>> source)
        {
            foreach (var p in source)
            {
                Items[p.Key] = p.Value;
            }
        }

        /// <summary>
        ///     Add items from <paramref name="source"/> to <see cref="TransferrableItems" />.
        /// </summary>
        /// <param name="source">The items to add.</param>
        void AddTransferrableItems(IEnumerable<KeyValuePair<string, string>> source)
        {
            foreach (var p in source)
            {
                TransferrableItems[p.Key] = p.Value;
            }
        }

        /*
        /// <summary>
        ///     Notifies when the command is aborted.
        /// </summary>
        CancellationToken CommandAborted { get; set; }

        /// <summary>
        ///     Gets or sets a unique identifier to represent this command in trace logs.
        /// </summary>
        string TraceIdentifier { get; set; }

        /// <summary>
        ///     Aborts the command.
        /// </summary>
        void Abort();
        */
    }
}
