using System;
using Conqueror.CQS.CommandHandling;

// ReSharper disable once CheckNamespace (we want these extensions to be accessible from client registration code without an extra import)
namespace Conqueror;

public static class InMemoryCommandTransportClientBuilderExtensions
{
    public static ICommandTransportClient UseInMemory(this ICommandTransportClientBuilder builder)
    {
        return new InMemoryCommandTransport(typeof(ICommandHandler<,>).MakeGenericType(builder.CommandType, builder.ResponseType), null);
    }

    public static ICommandTransportClient UseInMemory<TCommand, TResponse>(this ICommandTransportClientBuilder builder,
                                                                           Action<ICommandPipeline<TCommand, TResponse>> configure)
        where TCommand : class
    {
        return new InMemoryCommandTransport(typeof(ICommandHandler<,>).MakeGenericType(builder.CommandType, builder.ResponseType), configure);
    }

    public static ICommandTransportClient UseInMemory<TCommand>(this ICommandTransportClientBuilder builder,
                                                                Action<ICommandPipeline<TCommand>> configure)
        where TCommand : class
    {
        return new InMemoryCommandTransport(typeof(ICommandHandler<,>).MakeGenericType(builder.CommandType, builder.ResponseType), configure);
    }
}
