using System.Collections.Generic;

namespace Conqueror.CQS
{
    /// <summary>
    ///     Allows setting contextual information for a command execution.
    /// </summary>
    public interface ICommandClientContext
    {
        /// <summary>
        ///     Makes the command client context active. The returned key/value collection can be used
        ///     to share contextual data for command executions. Command executions will have access to
        ///     the items and can also update the items with their own contextual response information.
        ///     The context can be used for multiple successive command executions, and contextual data
        ///     will flow across them. The data will also flow across transport boundaries (e.g HTTP).
        /// </summary>
        IDictionary<string, string> Activate();
        
        /// <summary>
        ///     Returns whether the command client context is currently active.
        /// </summary>
        bool IsActive { get; }
    }
}
