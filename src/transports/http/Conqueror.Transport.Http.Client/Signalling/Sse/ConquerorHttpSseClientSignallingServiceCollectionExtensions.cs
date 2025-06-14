using System;
using System.Threading.Tasks;
using Conqueror;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorHttpSseClientSignallingServiceCollectionExtensions
{
    public static IServiceCollection AddHttpSseSignalHandlerDelegate<TSignal, TIHandler>(
        this IServiceCollection services,
        SignalTypes<TSignal, TIHandler> messageTypes,
        SignalHandlerFn<TSignal> fn,
        Action<IHttpSseSignalReceiver> configureReceiver)
        where TSignal : class, IHttpSseSignal<TSignal>
        where TIHandler : class, IHttpSseSignalHandler<TSignal, TIHandler>
    {
        var typesInjector = new HttpSseSignalHandlerTypesInjector<TSignal, TIHandler>(configureReceiver);

        return services.AddSignalHandlerDelegate(
            messageTypes,
            fn,
            null,
            typesInjector);
    }

    public static IServiceCollection AddHttpSseSignalHandlerDelegate<TSignal, TIHandler>(
        this IServiceCollection services,
        SignalTypes<TSignal, TIHandler> messageTypes,
        SignalHandlerSyncFn<TSignal> fn,
        Action<IHttpSseSignalReceiver> configureReceiver)
        where TSignal : class, IHttpSseSignal<TSignal>
        where TIHandler : class, IHttpSseSignalHandler<TSignal, TIHandler>
    {
        var typesInjector = new HttpSseSignalHandlerTypesInjector<TSignal, TIHandler>(configureReceiver);

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

    public static IServiceCollection AddHttpSseSignalHandlerDelegate<TSignal, TIHandler>(
        this IServiceCollection services,
        SignalTypes<TSignal, TIHandler> messageTypes,
        SignalHandlerFn<TSignal> fn,
        Action<ISignalPipeline<TSignal>> configurePipeline,
        Action<IHttpSseSignalReceiver> configureReceiver)
        where TSignal : class, IHttpSseSignal<TSignal>
        where TIHandler : class, IHttpSseSignalHandler<TSignal, TIHandler>
    {
        var typesInjector = new HttpSseSignalHandlerTypesInjector<TSignal, TIHandler>(configureReceiver);

        return services.AddSignalHandlerDelegate(
            messageTypes,
            fn,
            configurePipeline,
            typesInjector);
    }

    public static IServiceCollection AddHttpSseSignalHandlerDelegate<TSignal, TIHandler>(
        this IServiceCollection services,
        SignalTypes<TSignal, TIHandler> messageTypes,
        SignalHandlerSyncFn<TSignal> fn,
        Action<ISignalPipeline<TSignal>> configurePipeline,
        Action<IHttpSseSignalReceiver> configureReceiver)
        where TSignal : class, IHttpSseSignal<TSignal>
        where TIHandler : class, IHttpSseSignalHandler<TSignal, TIHandler>
    {
        var typesInjector = new HttpSseSignalHandlerTypesInjector<TSignal, TIHandler>(configureReceiver);

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
