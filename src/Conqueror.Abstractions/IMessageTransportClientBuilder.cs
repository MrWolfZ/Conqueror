using System;

namespace Conqueror;

public interface IMessageTransportClientBuilder
{
    IServiceProvider ServiceProvider { get; }

    ConquerorContext ConquerorContext { get; }

    Type MessageType { get; }

    Type ResponseType { get; }
}
