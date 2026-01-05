#pragma warning disable IDE0130 // Namespaces don't match folder structure - we want these extensions to be accessible from client registration code without an extra import

namespace Conqueror;

public static class HttpWebSocketsIteratorClientBuilderExtensions
{
    public static IHttpWebSocketsIteratorClient<TIterator, TItem> UseHttpWebSockets<TIterator, TItem>(
        this IteratorClientBuilder<TIterator, TItem> builder
    )
        where TIterator : class, IHttpWebSocketsIterator<TIterator, TItem>
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (
            builder.ServiceProvider.GetService(typeof(IHttpWebSocketsIteratorClientFactory))
            is not IHttpWebSocketsIteratorClientFactory clientFactory
        )
        {
            throw new InvalidOperationException(
                $"could not resolve '{typeof(IHttpWebSocketsIteratorClientFactory)}'; did you forget to add the Conqueror HTTP client to the service collection?"
            );
        }

        return clientFactory.Get<TIterator, TItem>();
    }
}
