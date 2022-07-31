using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Conqueror.CQS.CommandHandling
{
    /// <inheritdoc />
    internal sealed class DefaultConquerorContext : IConquerorContext
    {
        /// <inheritdoc />
        public IDictionary<string, string> Items { get; } = new ConcurrentDictionary<string, string>();
    }
}
