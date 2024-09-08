using System;

namespace Conqueror.CQS.CommandHandling;

internal sealed class CommandTransportClientBuilder : ICommandTransportClientBuilder
{
    public CommandTransportClientBuilder(IServiceProvider serviceProvider, Type commandType, Type responseType)
    {
        ServiceProvider = serviceProvider;
        CommandType = commandType;
        ResponseType = responseType;
    }

    public IServiceProvider ServiceProvider { get; }

    public Type CommandType { get; }

    public Type ResponseType { get; }
}
