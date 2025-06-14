using System;
using System.Threading.Tasks;
using Conqueror;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorHttpWebSocketsClientSignallingServiceCollectionExtensions
{
    public static IServiceCollection AddHttpWebSocketsSignalHandlerDelegate<TSignal, TIHandler>(
        this IServiceCollection services,
        SignalTypes<TSignal, TIHandler> messageTypes,
        SignalHandlerFn<TSignal> fn,
        Action<IHttpWebSocketsSignalReceiver> configureReceiver)
        where TSignal : class, IHttpWebSocketsSignal<TSignal>
        where TIHandler : class, IHttpWebSocketsSignalHandler<TSignal, TIHandler>
    {
        var typesInjector = new HttpWebSocketsSignalHandlerTypesInjector<TSignal, TIHandler>(configureReceiver);

        return services.AddSignalHandlerDelegate(
            messageTypes,
            fn,
            null,
            typesInjector);
    }

    public static IServiceCollection AddHttpWebSocketsSignalHandlerDelegate<TSignal, TIHandler>(
        this IServiceCollection services,
        SignalTypes<TSignal, TIHandler> messageTypes,
        SignalHandlerSyncFn<TSignal> fn,
        Action<IHttpWebSocketsSignalReceiver> configureReceiver)
        where TSignal : class, IHttpWebSocketsSignal<TSignal>
        where TIHandler : class, IHttpWebSocketsSignalHandler<TSignal, TIHandler>
    {
        var typesInjector = new HttpWebSocketsSignalHandlerTypesInjector<TSignal, TIHandler>(configureReceiver);

        return services.AddSignalHandlerDelegate(
            messageTypes,
            (m, p, _) =>
            {
                fn(m, p);

                return Task.CompletedTask;
            },
            null,
            typesInjector);
    }

    public static IServiceCollection AddHttpWebSocketsSignalHandlerDelegate<TSignal, TIHandler>(
        this IServiceCollection services,
        SignalTypes<TSignal, TIHandler> messageTypes,
        SignalHandlerFn<TSignal> fn,
        Action<ISignalPipeline<TSignal>> configurePipeline,
        Action<IHttpWebSocketsSignalReceiver> configureReceiver)
        where TSignal : class, IHttpWebSocketsSignal<TSignal>
        where TIHandler : class, IHttpWebSocketsSignalHandler<TSignal, TIHandler>
    {
        var typesInjector = new HttpWebSocketsSignalHandlerTypesInjector<TSignal, TIHandler>(configureReceiver);

        return services.AddSignalHandlerDelegate(
            messageTypes,
            fn,
            configurePipeline,
            typesInjector);
    }

    public static IServiceCollection AddHttpWebSocketsSignalHandlerDelegate<TSignal, TIHandler>(
        this IServiceCollection services,
        SignalTypes<TSignal, TIHandler> messageTypes,
        SignalHandlerSyncFn<TSignal> fn,
        Action<ISignalPipeline<TSignal>> configurePipeline,
        Action<IHttpWebSocketsSignalReceiver> configureReceiver)
        where TSignal : class, IHttpWebSocketsSignal<TSignal>
        where TIHandler : class, IHttpWebSocketsSignalHandler<TSignal, TIHandler>
    {
        var typesInjector = new HttpWebSocketsSignalHandlerTypesInjector<TSignal, TIHandler>(configureReceiver);

        return services.AddSignalHandlerDelegate(
            messageTypes,
            (m, p, _) =>
            {
                fn(m, p);

                return Task.CompletedTask;
            },
            configurePipeline,
            typesInjector);
    }
}
