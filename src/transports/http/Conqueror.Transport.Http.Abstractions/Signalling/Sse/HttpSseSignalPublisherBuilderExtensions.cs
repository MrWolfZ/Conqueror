using System;

// ReSharper disable once CheckNamespace (we want these extensions to be accessible from client registration code without an extra import)
namespace Conqueror;

public static class HttpSseSignalPublisherBuilderExtensions
{
    public static IHttpSseSignalPublisher<TSignal> UseHttpServerSentEvents<TSignal>(
        this ISignalPublisherBuilder<TSignal> builder)
        where TSignal : class, IHttpSseSignal<TSignal>
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (builder.ServiceProvider.GetService(typeof(IHttpSseSignalPublisherFactory)) is not IHttpSseSignalPublisherFactory publisherFactory)
        {
            throw new InvalidOperationException(
                $"could not resolve '{typeof(IHttpSseSignalPublisherFactory)}'; did you forget to add the Conqueror HTTP server to the service collection?");
        }

        return publisherFactory.Get<TSignal>();
    }
}
