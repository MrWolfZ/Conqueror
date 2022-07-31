using System.Collections.Concurrent;
using System.Collections.Generic;

// TODO: move to common package
namespace Conqueror.CQS
{
    /// <inheritdoc />
    internal sealed class DefaultConquerorContext : IConquerorContext
    {
        /// <inheritdoc />
        public IDictionary<string, string> Items { get; } = new ConcurrentDictionary<string, string>();
    }
}
