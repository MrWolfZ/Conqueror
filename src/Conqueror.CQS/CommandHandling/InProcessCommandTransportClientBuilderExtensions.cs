using System;
using Conqueror.CQS.CommandHandling;

// ReSharper disable once CheckNamespace (we want these extensions to be accessible from client registration code without an extra import)
namespace Conqueror;

public static class InProcessCommandTransportClientBuilderExtensions
{
    public static ICommandTransportClient UseInProcess(this ICommandTransportClientBuilder builder)
    {
        return new InProcessCommandTransport(typeof(ICommandHandler<,>).MakeGenericType(builder.CommandType, builder.ResponseType), null);
    }

    public static ICommandTransportClient UseInProcess<TCommand, TResponse>(this ICommandTransportClientBuilder builder,
                                                                            Action<ICommandPipeline<TCommand, TResponse>> configure)
        where TCommand : class
    {
        return new InProcessCommandTransport(typeof(ICommandHandler<,>).MakeGenericType(builder.CommandType, builder.ResponseType), configure);
    }

    public static ICommandTransportClient UseInProcess<TCommand>(this ICommandTransportClientBuilder builder,
                                                                 Action<ICommandPipeline<TCommand>> configure)
        where TCommand : class
    {
        return new InProcessCommandTransport(typeof(ICommandHandler<,>).MakeGenericType(builder.CommandType, builder.ResponseType), configure);
    }
}
