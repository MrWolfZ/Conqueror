using Conqueror.Transport.Http.Server.AspNetCore.Signalling.Sse;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace (we want these extensions to be accessible from client registration code without an extra import)
namespace Conqueror;

public static class HttpSseSignalPublisherBuilderExtensions
{
    public static IHttpSseSignalPublisher<TSignal> UseHttpServerSentEvents<TSignal>(
        this ISignalPublisherBuilder<TSignal> builder)
        where TSignal : class, IHttpSseSignal<TSignal>
    {
        var singletons = builder.ServiceProvider.GetRequiredService<ConquerorSingletons>();
        var serverTransport = singletons.GetOrAddSingleton(p => new HttpSseSignalBroker(p));

        return new HttpSseSignalPublisher<TSignal>(serverTransport);
    }
}
