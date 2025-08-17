namespace Conqueror.Streaming;

internal sealed class StreamProducerMiddlewareRegistry(IEnumerable<IStreamProducerMiddlewareInvoker> invokers)
{
    private readonly IReadOnlyDictionary<Type, IStreamProducerMiddlewareInvoker> invokers = invokers.ToDictionary(i =>
        i.MiddlewareType
    );

    public IStreamProducerMiddlewareInvoker? GetStreamProducerMiddlewareInvoker<TMiddleware>()
        where TMiddleware : IStreamProducerMiddlewareMarker => invokers.GetValueOrDefault(typeof(TMiddleware));
}
