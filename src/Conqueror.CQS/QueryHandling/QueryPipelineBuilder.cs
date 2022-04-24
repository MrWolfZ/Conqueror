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

        public IQueryPipelineBuilder Without<TMiddleware>()
            where TMiddleware : IQueryMiddleware
        {
            var index = middlewares.FindIndex(tuple => tuple.MiddlewareType == typeof(TMiddleware));

            if (index < 0)
            {
                return this;
            }
            
            middlewares.RemoveAt(index);
            
            return this;
        }

        public IQueryPipelineBuilder Configure<TMiddleware, TConfiguration>(TConfiguration configuration)
            where TMiddleware : IQueryMiddleware<TConfiguration>
        {
            return Configure<TMiddleware, TConfiguration>(_ => configuration);
        }

        public IQueryPipelineBuilder Configure<TMiddleware, TConfiguration>(Action<TConfiguration> configure)
            where TMiddleware : IQueryMiddleware<TConfiguration>
        {
            return Configure<TMiddleware, TConfiguration>(c =>
            {
                configure(c);
                return c;
            });
        }

        public IQueryPipelineBuilder Configure<TMiddleware, TConfiguration>(Func<TConfiguration, TConfiguration> configure)
            where TMiddleware : IQueryMiddleware<TConfiguration>
        {
            var index = middlewares.FindIndex(tuple => tuple.MiddlewareType == typeof(TMiddleware));

            if (index < 0)
            {
                throw new InvalidOperationException($"middleware ${typeof(TMiddleware).Name} cannot be configured for this pipeline since it is not used");
            }
            
            middlewares[index] = (typeof(TMiddleware), configure((TConfiguration)middlewares[index].MiddlewareConfiguration!));
            return this;
        }

        public QueryPipeline Build()
        {
            return new(middlewares);
        }
    }
}
