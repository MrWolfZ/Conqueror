using System;
using System.Collections.Generic;

namespace Conqueror.CQS.CommandHandling;

internal sealed class CommandPipelineBuilder : ICommandPipelineBuilder
{
    private readonly CommandMiddlewareRegistry commandMiddlewareRegistry;
    private readonly List<(Type MiddlewareType, object? MiddlewareConfiguration, ICommandMiddlewareInvoker Invoker)> middlewares = new();

    public CommandPipelineBuilder(IServiceProvider serviceProvider, CommandMiddlewareRegistry commandMiddlewareRegistry)
    {
        this.commandMiddlewareRegistry = commandMiddlewareRegistry;
        ServiceProvider = serviceProvider;
    }

    public IServiceProvider ServiceProvider { get; }

    public ICommandPipelineBuilder Use<TMiddleware>()
        where TMiddleware : ICommandMiddleware
    {
        middlewares.Add((typeof(TMiddleware), null, GetInvoker<TMiddleware>()));
        return this;
    }

    public ICommandPipelineBuilder Use<TMiddleware, TConfiguration>(TConfiguration configuration)
        where TMiddleware : ICommandMiddleware<TConfiguration>
    {
        middlewares.Add((typeof(TMiddleware), configuration, GetInvoker<TMiddleware>()));
        return this;
    }

    public ICommandPipelineBuilder Without<TMiddleware>()
        where TMiddleware : ICommandMiddleware
    {
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

    public CommandPipeline Build(IConquerorContext conquerorContext)
    {
        return new(conquerorContext, middlewares);
    }

    private ICommandMiddlewareInvoker GetInvoker<TMiddleware>()
        where TMiddleware : ICommandMiddlewareMarker
    {
        if (commandMiddlewareRegistry.GetCommandMiddlewareInvoker<TMiddleware>() is { } invoker)
        {
            return invoker;
        }

        throw new InvalidOperationException(
            $"trying to use unregistered middleware type '{typeof(TMiddleware).Name}' in pipeline; ensure that the middleware is registered in the DI container");
    }
}
