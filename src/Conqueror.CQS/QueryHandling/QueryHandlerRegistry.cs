using System;
using System.Collections.Generic;
using System.Linq;

namespace Conqueror.CQS.QueryHandling
{
    internal sealed class QueryHandlerRegistry
    {
        private readonly IReadOnlyDictionary<(Type QueryType, Type ResponseType), QueryHandlerMetadata> metadataLookup;

        public QueryHandlerRegistry(IEnumerable<QueryHandlerMetadata> metadata)
        {
            metadataLookup = metadata.ToDictionary(m => (m.QueryType, m.ResponseType));
        }

        public QueryHandlerMetadata GetQueryHandlerMetadata<TQuery, TResponse>()
            where TQuery : class
        {
            if (!metadataLookup.TryGetValue((typeof(TQuery), typeof(TResponse)), out var metadata))
            {
                throw new ArgumentException($"there is no registered query handler for query type {typeof(TQuery).Name} and response type {typeof(TResponse).Name}");
            }

            return metadata;
        }
    }
}
