using System.Collections.Generic;

namespace Conqueror.CQS
{
    /// <summary>
    ///     Encapsulates all HTTP-specific information about an individual HTTP request.
    /// </summary>
    public abstract class CommandContext
    {
        /// <summary>
        ///     Gets the command object.
        /// </summary>
        public abstract object Command { get; }

        /// <summary>
        ///     Gets the response object (if it is already set).
        /// </summary>
        public abstract object? Response { get; }

        /// <summary>
        ///     Gets or sets a key/value collection that can be used to share data within the scope of this command.
        /// </summary>
        public abstract IDictionary<object, object?> Items { get; set; }

        /// <summary>
        ///     Gets or sets a key/value collection that can be used to share data across different commands or transport boundaries.
        /// </summary>
        public abstract IDictionary<string, string> TransferrableItems { get; set; }

        /*
        /// <summary>
        ///     Notifies when the command is aborted.
        /// </summary>
        public abstract CancellationToken CommandAborted { get; set; }

        /// <summary>
        ///     Gets or sets a unique identifier to represent this command in trace logs.
        /// </summary>
        public abstract string TraceIdentifier { get; set; }

        /// <summary>
        ///     Aborts the command.
        /// </summary>
        public abstract void Abort();
        */
    }
}
