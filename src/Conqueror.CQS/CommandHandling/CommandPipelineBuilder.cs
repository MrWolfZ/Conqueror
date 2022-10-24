using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.CQS.CommandHandling
{
    internal sealed class CommandPipelineBuilder : ICommandPipelineBuilder
    {
        private readonly IReadOnlyDictionary<Type, ICommandMiddlewareInvoker> middlewareInvokersByMiddlewareTypes;
        private readonly List<(Type MiddlewareType, object? MiddlewareConfiguration, ICommandMiddlewareInvoker Invoker)> middlewares = new();
        private readonly Dictionary<Type, MiddlewareRegistration> registeredMiddlewareTypes = new();

        public CommandPipelineBuilder(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
            middlewareInvokersByMiddlewareTypes = serviceProvider.GetRequiredService<IReadOnlyDictionary<Type, ICommandMiddlewareInvoker>>();
        }

        public IServiceProvider ServiceProvider { get; }

        public ICommandPipelineBuilder Use<TMiddleware>()
            where TMiddleware : ICommandMiddleware
        {
            if (!registeredMiddlewareTypes.TryAdd(typeof(TMiddleware), new()))
            {
                throw new InvalidOperationException($"middleware '{typeof(TMiddleware).Name}' is already registered");
            }

            middlewares.Add((typeof(TMiddleware), null, GetInvoker<TMiddleware>()));
            return this;
        }

        public ICommandPipelineBuilder Use<TMiddleware, TConfiguration>(TConfiguration configuration)
            where TMiddleware : ICommandMiddleware<TConfiguration>
        {
            if (!registeredMiddlewareTypes.TryAdd(typeof(TMiddleware), new()))
            {
                throw new InvalidOperationException($"middleware '{typeof(TMiddleware).Name}' is already registered");
            }

            middlewares.Add((typeof(TMiddleware), configuration, GetInvoker<TMiddleware>()));
            return this;
        }

        public ICommandPipelineBuilder UseAllowMultiple<TMiddleware>()
            where TMiddleware : ICommandMiddleware
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

        public ICommandPipelineBuilder UseAllowMultiple<TMiddleware, TConfiguration>(TConfiguration configuration)
            where TMiddleware : ICommandMiddleware<TConfiguration>
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

        public ICommandPipelineBuilder Without<TMiddleware>()
            where TMiddleware : ICommandMiddleware
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

        public ICommandPipelineBuilder Without<TMiddleware, TConfiguration>()
            where TMiddleware : ICommandMiddleware<TConfiguration>
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

        public ICommandPipelineBuilder Configure<TMiddleware, TConfiguration>(TConfiguration configuration)
            where TMiddleware : ICommandMiddleware<TConfiguration>
        {
            return Configure<TMiddleware, TConfiguration>(_ => configuration);
        }

        public ICommandPipelineBuilder Configure<TMiddleware, TConfiguration>(Action<TConfiguration> configure)
            where TMiddleware : ICommandMiddleware<TConfiguration>
        {
            return Configure<TMiddleware, TConfiguration>(c =>
            {
                configure(c);
                return c;
            });
        }

        public ICommandPipelineBuilder Configure<TMiddleware, TConfiguration>(Func<TConfiguration, TConfiguration> configure)
            where TMiddleware : ICommandMiddleware<TConfiguration>
        {
            var index = middlewares.FindIndex(tuple => tuple.MiddlewareType == typeof(TMiddleware));

            if (index < 0)
            {
                throw new InvalidOperationException($"middleware ${typeof(TMiddleware).Name} cannot be configured for this pipeline since it is not used");
            }

            middlewares[index] = (typeof(TMiddleware), configure((TConfiguration)middlewares[index].MiddlewareConfiguration!), GetInvoker<TMiddleware>());
            return this;
        }

        public CommandPipeline Build()
        {
            return new(ServiceProvider.GetRequiredService<CommandContextAccessor>(),
                       ServiceProvider.GetRequiredService<ConquerorContextAccessor>(),
                       middlewares.Select(t => (t.MiddlewareConfiguration, t.Invoker)));
        }

        private ICommandMiddlewareInvoker GetInvoker<TMiddleware>()
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
