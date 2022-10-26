using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Conqueror
{
    /// <inheritdoc />
    internal sealed class DefaultConquerorContext : IConquerorContext
    {
        private readonly Lazy<IDictionary<string, string>> itemsLazy = new(() => new ConcurrentDictionary<string, string>());

        /// <inheritdoc />
        public IDictionary<string, string> Items => itemsLazy.Value;
    }
}
