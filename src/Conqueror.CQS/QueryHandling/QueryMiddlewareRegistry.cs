using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.CQS.QueryHandling
{
    internal sealed class QueryMiddlewareRegistry
    {
        private readonly IReadOnlyDictionary<Type, QueryMiddlewareMetadata> metadataLookup;

        public QueryMiddlewareRegistry(IEnumerable<QueryMiddlewareMetadata> metadata)
        {
            metadataLookup = metadata.ToDictionary(m => m.AttributeType);
        }

        public IQueryMiddleware<TConfiguration> GetMiddleware<TConfiguration>(IServiceProvider serviceProvider)
            where TConfiguration : QueryMiddlewareConfigurationAttribute
        {
            if (!metadataLookup.TryGetValue(typeof(TConfiguration), out var metadata))
            {
                throw new ArgumentException($"there is no registered query middleware for attribute {typeof(TConfiguration).Name}");
            }

            return (IQueryMiddleware<TConfiguration>)serviceProvider.GetRequiredService(metadata.MiddlewareType);
        }
    }
}
