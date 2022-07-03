using System;
using System.Collections.Generic;

namespace Conqueror.Eventing
{
    internal sealed class EventObserverPipelineBuilder : IEventObserverPipelineBuilder
    {
        private readonly List<(Type MiddlewareType, object? MiddlewareConfiguration)> middlewares = new();
        
        public EventObserverPipelineBuilder(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        public IServiceProvider ServiceProvider { get; }

        public IEventObserverPipelineBuilder Use<TMiddleware>()
            where TMiddleware : IEventObserverMiddleware
        {
            middlewares.Add((typeof(TMiddleware), null));
            return this;
        }

        public IEventObserverPipelineBuilder Use<TMiddleware, TConfiguration>(TConfiguration configuration)
            where TMiddleware : IEventObserverMiddleware<TConfiguration>
        {
            middlewares.Add((typeof(TMiddleware), configuration));
            return this;
        }

        public IEventObserverPipelineBuilder Without<TMiddleware>()
            where TMiddleware : IEventObserverMiddleware
        {
            var index = middlewares.FindIndex(tuple => tuple.MiddlewareType == typeof(TMiddleware));

            if (index < 0)
            {
                return this;
            }
            
            middlewares.RemoveAt(index);
            
            return this;
        }

        public IEventObserverPipelineBuilder Configure<TMiddleware, TConfiguration>(TConfiguration configuration)
            where TMiddleware : IEventObserverMiddleware<TConfiguration>
        {
            return Configure<TMiddleware, TConfiguration>(_ => configuration);
        }

        public IEventObserverPipelineBuilder Configure<TMiddleware, TConfiguration>(Action<TConfiguration> configure)
            where TMiddleware : IEventObserverMiddleware<TConfiguration>
        {
            return Configure<TMiddleware, TConfiguration>(c =>
            {
                configure(c);
                return c;
            });
        }

        public IEventObserverPipelineBuilder Configure<TMiddleware, TConfiguration>(Func<TConfiguration, TConfiguration> configure)
            where TMiddleware : IEventObserverMiddleware<TConfiguration>
        {
            var index = middlewares.FindIndex(tuple => tuple.MiddlewareType == typeof(TMiddleware));

            if (index < 0)
            {
                throw new InvalidOperationException($"middleware ${typeof(TMiddleware).Name} cannot be configured for this pipeline since it is not used");
            }
            
            middlewares[index] = (typeof(TMiddleware), configure((TConfiguration)middlewares[index].MiddlewareConfiguration!));
            return this;
        }

        public EventObserverPipeline Build()
        {
            return new(middlewares);
        }
    }
}
