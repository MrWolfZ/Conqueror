using System;
using System.Collections.Generic;

namespace Conqueror.CQS.QueryHandling
{
    internal sealed class QueryPipelineBuilder : IQueryPipelineBuilder
    {
        private readonly List<(Type MiddlewareType, object? MiddlewareConfiguration)> middlewares = new();

        public IQueryPipelineBuilder Use<TMiddleware>()
            where TMiddleware : IQueryMiddleware
        {
            middlewares.Add((typeof(TMiddleware), null));
            return this;
        }

        public IQueryPipelineBuilder Use<TMiddleware, TConfiguration>(TConfiguration configuration)
            where TMiddleware : IQueryMiddleware<TConfiguration>
        {
            middlewares.Add((typeof(TMiddleware), configuration));
            return this;
        }

        public QueryPipeline Build()
        {
            return new(middlewares);
        }
    }
}
