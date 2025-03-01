using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Eventing.Publishing;

internal sealed class EventPublisherPipelineBuilder : IEventPublisherPipelineBuilder
{
    private readonly IReadOnlyDictionary<Type, IEventPublisherMiddlewareInvoker> middlewareInvokersByMiddlewareTypes;
    private readonly List<(Type MiddlewareType, object? MiddlewareConfiguration, IEventPublisherMiddlewareInvoker Invoker)> middlewares = [];

    public EventPublisherPipelineBuilder(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
        middlewareInvokersByMiddlewareTypes = serviceProvider.GetRequiredService<IEnumerable<IEventPublisherMiddlewareInvoker>>().ToDictionary(i => i.MiddlewareType);
    }

    public IServiceProvider ServiceProvider { get; }

    public IEventPublisherPipelineBuilder Use<TMiddleware>()
        where TMiddleware : IEventPublisherMiddleware
    {
        middlewares.Add((typeof(TMiddleware), null, GetInvoker<TMiddleware>()));
        return this;
    }

    public IEventPublisherPipelineBuilder Use<TMiddleware, TConfiguration>(TConfiguration configuration)
        where TMiddleware : IEventPublisherMiddleware<TConfiguration>
    {
        middlewares.Add((typeof(TMiddleware), configuration, GetInvoker<TMiddleware>()));
        return this;
    }

    public IEventPublisherPipelineBuilder Without<TMiddleware>()
        where TMiddleware : IEventPublisherMiddleware
    {
        var index = middlewares.FindIndex(tuple => tuple.MiddlewareType == typeof(TMiddleware));

        if (index < 0)
        {
            return this;
        }

        middlewares.RemoveAt(index);

        return this;
    }

    public IEventPublisherPipelineBuilder Without<TMiddleware, TConfiguration>()
        where TMiddleware : IEventPublisherMiddleware<TConfiguration>
    {
        var index = middlewares.FindIndex(tuple => tuple.MiddlewareType == typeof(TMiddleware));

        if (index < 0)
        {
            return this;
        }

        middlewares.RemoveAt(index);

        return this;
    }

    public IEventPublisherPipelineBuilder Configure<TMiddleware, TConfiguration>(TConfiguration configuration)
        where TMiddleware : IEventPublisherMiddleware<TConfiguration>
    {
        return Configure<TMiddleware, TConfiguration>(_ => configuration);
    }

    public IEventPublisherPipelineBuilder Configure<TMiddleware, TConfiguration>(Action<TConfiguration> configure)
        where TMiddleware : IEventPublisherMiddleware<TConfiguration>
    {
        return Configure<TMiddleware, TConfiguration>(c =>
        {
            configure(c);
            return c;
        });
    }

    public IEventPublisherPipelineBuilder Configure<TMiddleware, TConfiguration>(Func<TConfiguration, TConfiguration> configure)
        where TMiddleware : IEventPublisherMiddleware<TConfiguration>
    {
        var index = middlewares.FindIndex(tuple => tuple.MiddlewareType == typeof(TMiddleware));

        if (index < 0)
        {
            throw new InvalidOperationException($"middleware ${typeof(TMiddleware).Name} cannot be configured for this pipeline since it is not used");
        }

        middlewares[index] = (typeof(TMiddleware), configure((TConfiguration)middlewares[index].MiddlewareConfiguration!), GetInvoker<TMiddleware>());
        return this;
    }

    public EventPublisherPipeline<TConfiguration> Build<TConfiguration>()
        where TConfiguration : Attribute, IConquerorEventTransportConfigurationAttribute
    {
        return new(middlewares);
    }

    private IEventPublisherMiddlewareInvoker GetInvoker<TMiddleware>()
    {
        if (!middlewareInvokersByMiddlewareTypes.TryGetValue(typeof(TMiddleware), out var invoker))
        {
            throw new InvalidOperationException(
                $"trying to use unregistered middleware type '{typeof(TMiddleware).Name}' in pipeline; ensure that the middleware is registered in the DI container");
        }

        return invoker;
    }
}
