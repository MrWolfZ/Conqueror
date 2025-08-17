namespace Conqueror.Streaming;

internal sealed class StreamConsumerMiddlewareRegistry(IEnumerable<IStreamConsumerMiddlewareInvoker> invokers)
{
    private readonly IReadOnlyDictionary<Type, IStreamConsumerMiddlewareInvoker> invokers = invokers.ToDictionary(i =>
        i.MiddlewareType
    );

    public IStreamConsumerMiddlewareInvoker? GetStreamConsumerMiddlewareInvoker<TMiddleware>()
        where TMiddleware : IStreamConsumerMiddlewareMarker => invokers.GetValueOrDefault(typeof(TMiddleware));
}
