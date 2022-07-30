using System.Collections.Generic;

namespace Conqueror.CQS
{
    /// <summary>
    ///     Allows setting contextual information for a command execution.
    /// </summary>
    public interface ICommandClientContext
    {
        /// <summary>
        ///     Returns whether the command client context is currently active.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        ///     Makes the command client context active. The returned key/value collection can be used
        ///     to share contextual data for command executions. Command executions will have access to
        ///     the items and can also update the items with their own contextual response information.
        ///     The context can be used for multiple successive command executions, and contextual data
        ///     will flow across them. The data will also flow across transport boundaries (e.g HTTP).
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
