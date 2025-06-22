using System;

// ReSharper disable once CheckNamespace (we want these extensions to be accessible from client registration code without an extra import)
namespace Conqueror;

public static class HttpWebSocketsSignalPublisherBuilderExtensions
{
    public static IHttpWebSocketsSignalPublisher<TSignal> UseHttpWebSockets<TSignal>(
        this SignalPublisherBuilder<TSignal> builder)
        where TSignal : class, IHttpWebSocketsSignal<TSignal>
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (builder.ServiceProvider.GetService(typeof(IHttpWebSocketsSignalPublisherFactory)) is not IHttpWebSocketsSignalPublisherFactory publisherFactory)
        {
            throw new InvalidOperationException(
                $"could not resolve '{typeof(IHttpWebSocketsSignalPublisherFactory)}'; did you forget to add the Conqueror HTTP server to the service collection?");
        }

        return publisherFactory.Get<TSignal>();
    }
}
