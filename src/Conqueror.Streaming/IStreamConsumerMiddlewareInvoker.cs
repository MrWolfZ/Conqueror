namespace Conqueror.Streaming;

internal interface IStreamConsumerMiddlewareInvoker
{
    Type MiddlewareType { get; }

    Task Invoke<TItem>(
        TItem item,
        StreamConsumerMiddlewareNext<TItem> next,
        object? middlewareConfiguration,
        IServiceProvider serviceProvider,
        ConquerorContext conquerorContext,
        CancellationToken cancellationToken
    );
}
