namespace Conqueror;

public interface IStreamProducerTransportClient
{
    IAsyncEnumerable<TItem> ExecuteRequest<TRequest, TItem>(
        TRequest request,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken
    )
        where TRequest : class;
}
