using System.Collections.Generic;

namespace Conqueror;

public interface IStreamingRequestMiddleware : IStreamingRequestMiddlewareMarker
{
    IAsyncEnumerable<TItem> Execute<TRequest, TItem>(StreamingRequestMiddlewareContext<TRequest, TItem> ctx)
        where TRequest : class;
}

public interface IStreamingRequestMiddleware<TConfiguration> : IStreamingRequestMiddlewareMarker
{
    IAsyncEnumerable<TItem> Execute<TRequest, TItem>(StreamingRequestMiddlewareContext<TRequest, TItem, TConfiguration> ctx)
        where TRequest : class;
}

public interface IStreamingRequestMiddlewareMarker;
