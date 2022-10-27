using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Conqueror.CQS.QueryHandling
{
    /// <inheritdoc />
    internal sealed class DefaultQueryContext : IQueryContext
    {
        private readonly Lazy<IDictionary<object, object?>> itemsLazy = new(() => new ConcurrentDictionary<object, object?>());

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
        public IDictionary<object, object?> Items => itemsLazy.Value;

        public void SetQuery(object qry)
        {
            query = qry;
        }

        public void SetResponse(object res)
        {
            response = res;
        }
    }
}
