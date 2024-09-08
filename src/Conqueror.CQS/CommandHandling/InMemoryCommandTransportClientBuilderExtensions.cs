using Conqueror.CQS.CommandHandling;

// ReSharper disable once CheckNamespace (we want these extensions to be accessible from client registration code without an extra import)
namespace Conqueror;

public static class InMemoryCommandTransportClientBuilderExtensions
{
    public static ICommandTransportClient UseInMemory(this ICommandTransportClientBuilder builder)
    {
        return new InMemoryCommandTransport(typeof(ICommandHandler<,>).MakeGenericType(builder.CommandType, builder.ResponseType));
    }
}
