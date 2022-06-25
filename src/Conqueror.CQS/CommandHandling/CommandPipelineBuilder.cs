using System;
using System.Collections.Generic;

namespace Conqueror.CQS.CommandHandling
{
    internal sealed class CommandPipelineBuilder : ICommandPipelineBuilder
    {
        private readonly List<(Type MiddlewareType, object? MiddlewareConfiguration)> middlewares = new();
        
        public CommandPipelineBuilder(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        public IServiceProvider ServiceProvider { get; }

        public ICommandPipelineBuilder Use<TMiddleware>()
            where TMiddleware : ICommandMiddleware
        {
            middlewares.Add((typeof(TMiddleware), null));
            return this;
        }

        public ICommandPipelineBuilder Use<TMiddleware, TConfiguration>(TConfiguration configuration)
            where TMiddleware : ICommandMiddleware<TConfiguration>
        {
            middlewares.Add((typeof(TMiddleware), configuration));
            return this;
        }

        public ICommandPipelineBuilder Without<TMiddleware>()
            where TMiddleware : ICommandMiddleware
        {
            var index = middlewares.FindIndex(tuple => tuple.MiddlewareType == typeof(TMiddleware));

            if (index < 0)
            {
                return this;
            }
            
            middlewares.RemoveAt(index);
            
            return this;
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
            
            middlewares[index] = (typeof(TMiddleware), configure((TConfiguration)middlewares[index].MiddlewareConfiguration!));
            return this;
        }

        public CommandPipeline Build()
        {
            return new(middlewares);
        }
    }
}
