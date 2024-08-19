using System;

namespace Conqueror.Streaming;

internal sealed class StreamProducerTransportClientBuilder : IStreamProducerTransportClientBuilder
{
    public StreamProducerTransportClientBuilder(IServiceProvider serviceProvider, Type requestType)
    {
        ServiceProvider = serviceProvider;
        RequestType = requestType;
    }

    public IServiceProvider ServiceProvider { get; }

    public Type RequestType { get; }
}
