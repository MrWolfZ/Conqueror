using System;
using System.Collections.Generic;
using Conqueror.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.CQS.QueryHandling
{
    internal sealed class QueryPipelineBuilder : IQueryPipelineBuilder
    {
        private readonly IReadOnlyDictionary<Type, IQueryMiddlewareInvoker> middlewareInvokersByMiddlewareTypes;
        private readonly List<(Type MiddlewareType, object? MiddlewareConfiguration, IQueryMiddlewareInvoker Invoker)> middlewares = new();
        private readonly Dictionary<Type, MiddlewareRegistration> registeredMiddlewareTypes = new();

        public QueryPipelineBuilder(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
            middlewareInvokersByMiddlewareTypes = serviceProvider.GetRequiredService<IReadOnlyDictionary<Type, IQueryMiddlewareInvoker>>();
        }

        public IServiceProvider ServiceProvider { get; }

        public IQueryPipelineBuilder Use<TMiddleware>()
            where TMiddleware : IQueryMiddleware
        {
            if (!registeredMiddlewareTypes.TryAdd(typeof(TMiddleware), new()))
            {
                throw new InvalidOperationException($"middleware '{typeof(TMiddleware).Name}' is already registered");
            }

            middlewares.Add((typeof(TMiddleware), null, GetInvoker<TMiddleware>()));
            return this;
        }

        public IQueryPipelineBuilder Use<TMiddleware, TConfiguration>(TConfiguration configuration)
            where TMiddleware : IQueryMiddleware<TConfiguration>
        {
            if (!registeredMiddlewareTypes.TryAdd(typeof(TMiddleware), new()))
            {
                throw new InvalidOperationException($"middleware '{typeof(TMiddleware).Name}' is already registered");
            }

            middlewares.Add((typeof(TMiddleware), configuration, GetInvoker<TMiddleware>()));
            return this;
        }

        public IQueryPipelineBuilder UseAllowMultiple<TMiddleware>()
            where TMiddleware : IQueryMiddleware
        {
            if (!registeredMiddlewareTypes.TryAdd(typeof(TMiddleware), new() { IsExclusive = false }))
            {
                if (registeredMiddlewareTypes[typeof(TMiddleware)].IsExclusive)
                {
                    throw new InvalidOperationException($"middleware '{typeof(TMiddleware).Name}' is already registered as exclusive");
                }

                registeredMiddlewareTypes[typeof(TMiddleware)].RegistrationCount += 1;
            }

            middlewares.Add((typeof(TMiddleware), null, GetInvoker<TMiddleware>()));
            return this;
        }

        public IQueryPipelineBuilder UseAllowMultiple<TMiddleware, TConfiguration>(TConfiguration configuration)
            where TMiddleware : IQueryMiddleware<TConfiguration>
        {
            if (!registeredMiddlewareTypes.TryAdd(typeof(TMiddleware), new() { IsExclusive = false }))
            {
                if (registeredMiddlewareTypes[typeof(TMiddleware)].IsExclusive)
                {
                    throw new InvalidOperationException($"middleware '{typeof(TMiddleware).Name}' is already registered as exclusive");
                }

                registeredMiddlewareTypes[typeof(TMiddleware)].RegistrationCount += 1;
            }

            middlewares.Add((typeof(TMiddleware), configuration, GetInvoker<TMiddleware>()));
            return this;
        }

        public IQueryPipelineBuilder Without<TMiddleware>()
            where TMiddleware : IQueryMiddleware
        {
            _ = registeredMiddlewareTypes.Remove(typeof(TMiddleware));

            while (true)
            {
                var index = middlewares.FindIndex(tuple => tuple.MiddlewareType == typeof(TMiddleware));

                if (index < 0)
                {
                    return this;
                }

                middlewares.RemoveAt(index);
            }
        }

        public IQueryPipelineBuilder Without<TMiddleware, TConfiguration>()
            where TMiddleware : IQueryMiddleware<TConfiguration>
        {
            _ = registeredMiddlewareTypes.Remove(typeof(TMiddleware));

            while (true)
            {
                var index = middlewares.FindIndex(tuple => tuple.MiddlewareType == typeof(TMiddleware));

                if (index < 0)
                {
                    return this;
                }

                middlewares.RemoveAt(index);
            }
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

            middlewares[index] = (typeof(TMiddleware), configure((TConfiguration)middlewares[index].MiddlewareConfiguration!), GetInvoker<TMiddleware>());
            return this;
        }

        public QueryPipeline Build()
        {
            return new(ServiceProvider.GetRequiredService<QueryContextAccessor>(),
                       ServiceProvider.GetRequiredService<ConquerorContextAccessor>(),
                       middlewares);
        }

        private IQueryMiddlewareInvoker GetInvoker<TMiddleware>()
        {
            if (!middlewareInvokersByMiddlewareTypes.TryGetValue(typeof(TMiddleware), out var invoker))
            {
                throw new InvalidOperationException(
                    $"trying to use unregistered middleware type '{typeof(TMiddleware).Name}' in pipeline; ensure that the middleware is registered in the DI container");
            }

            return invoker;
        }

        private sealed class MiddlewareRegistration
        {
            public bool IsExclusive { get; init; } = true;

            public int RegistrationCount { get; set; } = 1;
        }
    }
}
