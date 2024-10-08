using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Conqueror.CQS.CommandHandling;

internal sealed class CommandPipeline<TCommand, TResponse>(
    IServiceProvider serviceProvider,
    ConquerorContext conquerorContext,
    CommandTransportType transportType)
    : ICommandPipeline<TCommand, TResponse>
    where TCommand : class
{
    private readonly List<ICommandMiddleware<TCommand, TResponse>> middlewares = [];

    public IServiceProvider ServiceProvider { get; } = serviceProvider;

    public ConquerorContext ConquerorContext { get; } = conquerorContext;

    public CommandTransportType TransportType { get; } = transportType;

    public int Count => middlewares.Count;

    public ICommandPipeline<TCommand, TResponse> Use<TMiddleware>(TMiddleware middleware)
        where TMiddleware : ICommandMiddleware<TCommand, TResponse>
    {
        middlewares.Add(middleware);
        return this;
    }

    public ICommandPipeline<TCommand, TResponse> Use(CommandMiddlewareFn<TCommand, TResponse> middlewareFn)
    {
        return Use(new DelegateCommandMiddleware(middlewareFn));
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

    public IEnumerator<ICommandMiddleware<TCommand, TResponse>> GetEnumerator() => middlewares.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private sealed class DelegateCommandMiddleware(CommandMiddlewareFn<TCommand, TResponse> middlewareFn) : ICommandMiddleware<TCommand, TResponse>
    {
        public Task<TResponse> Execute(CommandMiddlewareContext<TCommand, TResponse> ctx) => middlewareFn(ctx);
    }
}
