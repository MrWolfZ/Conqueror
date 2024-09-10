using System;
using Conqueror.CQS.CommandHandling;

// ReSharper disable once CheckNamespace (we want these extensions to be accessible without an extra import)
namespace Conqueror;

public static class ConquerorCqsCommandClientExtensions
{
    public static ICommandHandler<TCommand, TResponse> WithPipeline<TCommand, TResponse>(this ICommandHandler<TCommand, TResponse> handler,
                                                                                         Action<ICommandPipeline<TCommand, TResponse>> configurePipeline)
        where TCommand : class
    {
        if (handler is CommandHandlerProxy<TCommand, TResponse> proxy)
        {
            return proxy.WithPipeline(configurePipeline);
        }

        if (handler is CommandHandlerGeneratedProxyBase<TCommand, TResponse> generatedProxy)
        {
            return generatedProxy.WithPipeline(configurePipeline);
        }

        throw new NotSupportedException($"handler type {handler.GetType()} not supported");
    }

    public static ICommandHandler<TCommand> WithPipeline<TCommand>(this ICommandHandler<TCommand> handler,
                                                                   Action<ICommandPipeline<TCommand>> configurePipeline)
        where TCommand : class
    {
        if (handler is CommandHandlerWithoutResponseAdapter<TCommand> adapter)
        {
            return adapter.WithPipeline(configurePipeline);
        }

        if (handler is CommandHandlerGeneratedProxyBase<TCommand> generatedProxy)
        {
            return generatedProxy.WithPipeline(configurePipeline);
        }

        throw new NotSupportedException($"handler type {handler.GetType()} not supported");
    }
}
