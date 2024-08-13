using System;

namespace Conqueror.Streaming;

internal sealed class StreamingRequestMiddlewareRegistry
{
    public void GetStreamingRequestMiddlewareInvoker<TMiddleware>()
        where TMiddleware : IStreamingRequestMiddlewareMarker
    {
        throw new NotImplementedException();
    }
}
