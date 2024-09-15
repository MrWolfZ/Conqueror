using System;

namespace Conqueror.Streaming;

internal sealed class StreamProducerTransportClientBuilder(IServiceProvider serviceProvider, Type requestType) : IStreamProducerTransportClientBuilder
{
    public IServiceProvider ServiceProvider { get; } = serviceProvider;

    public Type RequestType { get; } = requestType;
}
