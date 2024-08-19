using System;

namespace Conqueror;

public interface IStreamProducerTransportClientBuilder
{
    IServiceProvider ServiceProvider { get; }

    Type RequestType { get; }
}
