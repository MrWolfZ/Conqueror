using System.Collections.Generic;

// TODO: move to common abstractions package
namespace Conqueror.CQS
{
    /// <summary>
    ///     Allows setting contextual information for conqueror executions (i.e. commands, queries, and events).
    /// </summary>
    public interface IConquerorClientContext
    {
        /// <summary>
        ///     Returns whether the Conqueror client context is currently active.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        ///     Makes the client context active. The returned key/value collection can be used to share
        ///     contextual data for command, query, and event handler executions. Executions will have
        ///     access to the items and can also update the items with their own contextual information.
        ///     The context can be used for multiple successive executions, and contextual data will
        ///     flow across them. The data also flows across transport boundaries (e.g HTTP).
        /// </summary>
        /// <exception cref="System.InvalidOperationException">If the client context is already active.</exception>
        IDictionary<string, string> Activate();

        /// <summary>
        ///     Returns the context items. Generally it is recommended to instead use <see cref="Activate" />
        ///     to activate the context and capture the return value of that method to interact with the
        ///     items. This method allows interacting with the items of an already activated context further
        ///     down the call stack.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">If the client context is not active.</exception>
        IDictionary<string, string> GetItems();
    }
}
