using System;

namespace Conqueror.Streaming;

internal sealed class StreamingRequestTransportClientBuilder : IStreamingRequestTransportClientBuilder
{
    public StreamingRequestTransportClientBuilder(IServiceProvider serviceProvider, Type requestType)
    {
        ServiceProvider = serviceProvider;
        RequestType = requestType;
    }

    public IServiceProvider ServiceProvider { get; }

    public Type RequestType { get; }
}
