using System.Collections.Generic;

namespace Conqueror;

public interface IStreamProducerMiddleware : IStreamProducerMiddlewareMarker
{
    IAsyncEnumerable<TItem> Execute<TRequest, TItem>(StreamProducerMiddlewareContext<TRequest, TItem> ctx)
        where TRequest : class;
}

public interface IStreamProducerMiddleware<TConfiguration> : IStreamProducerMiddlewareMarker
{
    IAsyncEnumerable<TItem> Execute<TRequest, TItem>(StreamProducerMiddlewareContext<TRequest, TItem, TConfiguration> ctx)
        where TRequest : class;
}

public interface IStreamProducerMiddlewareMarker;
