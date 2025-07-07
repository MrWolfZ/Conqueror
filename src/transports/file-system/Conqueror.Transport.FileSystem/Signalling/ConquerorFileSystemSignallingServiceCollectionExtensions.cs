using Conqueror;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorFileSystemSignallingServiceCollectionExtensions
{
    public static IServiceCollection AddFileSystemSignalHandlerDelegate<TSignal, TIHandler>(
        this IServiceCollection services,
        SignalTypes<TSignal, TIHandler> messageTypes,
        SignalHandlerFn<TSignal> fn,
        Action<IFileSystemSignalReceiver> configureReceiver)
        where TSignal : class, IFileSystemSignal<TSignal>
        where TIHandler : class, IFileSystemSignalHandler<TSignal, TIHandler>
    {
        var typesInjector = new FileSystemSignalHandlerTypesInjector<TSignal, TIHandler>(configureReceiver);

        return services.AddSignalHandlerDelegate(
            messageTypes,
            fn,
            null,
            typesInjector);
    }

    public static IServiceCollection AddFileSystemSignalHandlerDelegate<TSignal, TIHandler>(
        this IServiceCollection services,
        SignalTypes<TSignal, TIHandler> messageTypes,
        SignalHandlerSyncFn<TSignal> fn,
        Action<IFileSystemSignalReceiver> configureReceiver)
        where TSignal : class, IFileSystemSignal<TSignal>
        where TIHandler : class, IFileSystemSignalHandler<TSignal, TIHandler>
    {
        var typesInjector = new FileSystemSignalHandlerTypesInjector<TSignal, TIHandler>(configureReceiver);

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

    public static IServiceCollection AddFileSystemSignalHandlerDelegate<TSignal, TIHandler>(
        this IServiceCollection services,
        SignalTypes<TSignal, TIHandler> messageTypes,
        SignalHandlerFn<TSignal> fn,
        Action<ISignalPipeline<TSignal>> configurePipeline,
        Action<IFileSystemSignalReceiver> configureReceiver)
        where TSignal : class, IFileSystemSignal<TSignal>
        where TIHandler : class, IFileSystemSignalHandler<TSignal, TIHandler>
    {
        var typesInjector = new FileSystemSignalHandlerTypesInjector<TSignal, TIHandler>(configureReceiver);

        return services.AddSignalHandlerDelegate(
            messageTypes,
            fn,
            configurePipeline,
            typesInjector);
    }

    public static IServiceCollection AddFileSystemSignalHandlerDelegate<TSignal, TIHandler>(
        this IServiceCollection services,
        SignalTypes<TSignal, TIHandler> messageTypes,
        SignalHandlerSyncFn<TSignal> fn,
        Action<ISignalPipeline<TSignal>> configurePipeline,
        Action<IFileSystemSignalReceiver> configureReceiver)
        where TSignal : class, IFileSystemSignal<TSignal>
        where TIHandler : class, IFileSystemSignalHandler<TSignal, TIHandler>
    {
        var typesInjector = new FileSystemSignalHandlerTypesInjector<TSignal, TIHandler>(configureReceiver);

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
