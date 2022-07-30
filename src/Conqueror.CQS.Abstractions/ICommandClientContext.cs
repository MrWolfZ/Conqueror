using System.Collections.Generic;

namespace Conqueror.CQS
{
    /// <summary>
    ///     Allows setting contextual information for a command execution.
    /// </summary>
    public interface ICommandClientContext
    {
        /// <summary>
        ///     Gets a key/value collection that can be used to set contextual data for a command execution. The
        ///     data will flow across transport boundaries.
        /// </summary>
        IDictionary<string, string> Items { get; }
        
        /// <summary>
        ///     Gets a key/value collection that contains contextual response data from a command execution. Note
        ///     that this collection will only be populated if <see cref="CaptureResponseItems" /> was called before
        ///     the command execution.
        /// </summary>
        IReadOnlyDictionary<string, string> ResponseItems { get; }

        /// <summary>
        ///     Signals to the context that it should capture contextual response data from a command execution.
        /// </summary>
        void CaptureResponseItems();
    }
}
