using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Conqueror.CQS.QueryHandling
{
    /// <inheritdoc />
    internal sealed class DefaultQueryContext : IQueryContext
    {
        private object query;
        private object? response;

        public DefaultQueryContext(object query)
        {
            this.query = query;
        }

        /// <inheritdoc />
        public object Query => query;

        /// <inheritdoc />
        public object? Response => response;

        /// <inheritdoc />
        public IDictionary<object, object?> Items { get; } = new ConcurrentDictionary<object, object?>();

        public void SetQuery(object cmd)
        {
            query = cmd;
        }

        public void SetResponse(object res)
        {
            response = res;
        }
    }
}
