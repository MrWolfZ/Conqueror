using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Conqueror.CQS.QueryHandling
{
    /// <inheritdoc />
    internal sealed class DefaultQueryContext : IQueryContext
    {
        private readonly Lazy<IDictionary<object, object?>> itemsLazy = new(() => new ConcurrentDictionary<object, object?>());

        public DefaultQueryContext(object query, string queryId)
        {
            Query = query;
            QueryId = queryId;
        }

        /// <inheritdoc />
        public object Query { get; private set; }

        /// <inheritdoc />
        public object? Response { get; private set; }

        /// <inheritdoc />
        public string QueryId { get; }

        /// <inheritdoc />
        public IDictionary<object, object?> Items => itemsLazy.Value;

        public void SetQuery(object query)
        {
            Query = query;
        }

        public void SetResponse(object? response)
        {
            Response = response;
        }
    }
}
