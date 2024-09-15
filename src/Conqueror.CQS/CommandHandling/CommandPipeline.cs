using System;
using System.Collections.Generic;
using System.Linq;

namespace Conqueror.CQS.CommandHandling;

internal sealed class CommandPipeline<TCommand, TResponse>(IServiceProvider serviceProvider) : ICommandPipeline<TCommand, TResponse>
    where TCommand : class
{
    private readonly List<ICommandMiddleware<TCommand, TResponse>> middlewares = [];

    public IServiceProvider ServiceProvider { get; } = serviceProvider;

    public ICommandPipeline<TCommand, TResponse> Use<TMiddleware>(TMiddleware middleware)
        where TMiddleware : ICommandMiddleware<TCommand, TResponse>
    {
        middlewares.Add(middleware);
        return this;
    }

    public ICommandPipeline<TCommand, TResponse> Without<TMiddleware>()
        where TMiddleware : ICommandMiddleware<TCommand, TResponse>
    {
        while (true)
        {
            var index = middlewares.FindIndex(m => m is TMiddleware);

            if (index < 0)
            {
                return this;
            }

            middlewares.RemoveAt(index);
        }
    }

    public ICommandPipeline<TCommand, TResponse> Configure<TMiddleware>(Action<TMiddleware> configure)
        where TMiddleware : ICommandMiddleware<TCommand, TResponse>
    {
        var index = middlewares.FindIndex(m => m is TMiddleware);

        if (index < 0)
        {
            throw new InvalidOperationException($"middleware '${typeof(TMiddleware)}' cannot be configured for this pipeline since it is not used");
        }

        foreach (var middleware in middlewares.OfType<TMiddleware>())
        {
            configure(middleware);
        }

        return this;
    }

    public CommandPipelineRunner<TCommand, TResponse> Build(ConquerorContext conquerorContext)
    {
        return new(conquerorContext, middlewares);
    }
}
