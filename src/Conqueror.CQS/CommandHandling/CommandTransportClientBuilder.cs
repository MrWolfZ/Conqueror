using System;

namespace Conqueror.CQS.CommandHandling;

internal sealed class CommandTransportClientBuilder(
    IServiceProvider serviceProvider,
    ConquerorContext conquerorContext,
    Type commandType,
    Type responseType)
    : ICommandTransportClientBuilder
{
    public IServiceProvider ServiceProvider { get; } = serviceProvider;

    public ConquerorContext ConquerorContext { get; } = conquerorContext;

    public Type CommandType { get; } = commandType;

    public Type ResponseType { get; } = responseType;
}
