using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Conqueror.Common
{
    /// <inheritdoc />
    internal sealed class DefaultConquerorContext : IConquerorContext
    {
        private readonly Lazy<IDictionary<string, string>> itemsLazy = new(() => new ConcurrentDictionary<string, string>());

        public DefaultConquerorContext(string traceId)
        {
            TraceId = traceId;
        }

        /// <inheritdoc />
        public IDictionary<string, string> Items => itemsLazy.Value;

        /// <inheritdoc />
        public bool HasItems => itemsLazy.IsValueCreated && Items.Count > 0;

        /// <inheritdoc />
        public string TraceId { get; private set; }

        /// <inheritdoc />
        public void SetTraceId(string traceId)
        {
            TraceId = traceId;
        }
    }
}
