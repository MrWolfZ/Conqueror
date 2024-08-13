using System;

namespace Conqueror;

public interface IStreamingRequestTransportClientBuilder
{
    IServiceProvider ServiceProvider { get; }

    Type RequestType { get; }
}
