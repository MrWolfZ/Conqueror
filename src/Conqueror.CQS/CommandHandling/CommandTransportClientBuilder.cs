using System;

namespace Conqueror.CQS.CommandHandling;

internal sealed class CommandTransportClientBuilder(
    IServiceProvider serviceProvider,
    Type commandType,
    Type responseType)
    : ICommandTransportClientBuilder
{
    public IServiceProvider ServiceProvider { get; } = serviceProvider;

    public Type CommandType { get; } = commandType;

    public Type ResponseType { get; } = responseType;
}
