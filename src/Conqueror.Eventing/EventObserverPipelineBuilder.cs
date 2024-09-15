using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Eventing;

internal sealed class EventObserverPipelineBuilder : IEventObserverPipelineBuilder
{
    private readonly IReadOnlyDictionary<Type, IEventObserverMiddlewareInvoker> middlewareInvokersByMiddlewareTypes;
    private readonly List<(Type MiddlewareType, object? MiddlewareConfiguration, IEventObserverMiddlewareInvoker Invoker)> middlewares = [];

    public EventObserverPipelineBuilder(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
        middlewareInvokersByMiddlewareTypes = serviceProvider.GetRequiredService<IEnumerable<IEventObserverMiddlewareInvoker>>().ToDictionary(i => i.MiddlewareType);
    }

    public IServiceProvider ServiceProvider { get; }

    public IEventObserverPipelineBuilder Use<TMiddleware>()
        where TMiddleware : IEventObserverMiddleware
    {
        middlewares.Add((typeof(TMiddleware), null, GetInvoker<TMiddleware>()));
        return this;
    }

    public IEventObserverPipelineBuilder Use<TMiddleware, TConfiguration>(TConfiguration configuration)
        where TMiddleware : IEventObserverMiddleware<TConfiguration>
    {
        middlewares.Add((typeof(TMiddleware), configuration, GetInvoker<TMiddleware>()));
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

    public IEventObserverPipelineBuilder Without<TMiddleware, TConfiguration>()
        where TMiddleware : IEventObserverMiddleware<TConfiguration>
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

        middlewares[index] = (typeof(TMiddleware), configure((TConfiguration)middlewares[index].MiddlewareConfiguration!), GetInvoker<TMiddleware>());
        return this;
    }

    public EventObserverPipeline Build()
    {
        return new(middlewares);
    }

    private IEventObserverMiddlewareInvoker GetInvoker<TMiddleware>()
    {
        if (!middlewareInvokersByMiddlewareTypes.TryGetValue(typeof(TMiddleware), out var invoker))
        {
            throw new InvalidOperationException(
                $"trying to use unregistered middleware type '{typeof(TMiddleware).Name}' in pipeline; ensure that the middleware is registered in the DI container");
        }

        return invoker;
    }
}
