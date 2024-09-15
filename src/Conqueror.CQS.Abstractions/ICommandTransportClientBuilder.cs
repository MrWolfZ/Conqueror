using System;

namespace Conqueror;

public interface ICommandTransportClientBuilder
{
    IServiceProvider ServiceProvider { get; }

    ConquerorContext ConquerorContext { get; }

    Type CommandType { get; }

    Type ResponseType { get; }
}
