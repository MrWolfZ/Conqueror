using System;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public static class SignalHandlerExtensions
{
    public static THandler WithPipeline<TSignal, THandler>(this ISignalHandler<TSignal, THandler> handler,
                                                           Action<ISignalPipeline<TSignal>> configurePipeline)
        where TSignal : class, ISignal<TSignal>
        where THandler : class, ISignalHandler<TSignal, THandler>
    {
        if (handler is IConfigurableSignalHandler<TSignal, THandler> c)
        {
            return c.WithPipeline(configurePipeline);
        }

        throw new ArgumentException($"handler type '{handler.GetType()}' is not supported in {nameof(WithPipeline)}", nameof(handler), null);
    }

    public static THandler WithPublisher<TSignal, THandler>(this ISignalHandler<TSignal, THandler> handler,
                                                            ConfigureSignalPublisher<TSignal> configurePublisher)
        where TSignal : class, ISignal<TSignal>
        where THandler : class, ISignalHandler<TSignal, THandler>
    {
        if (handler is IConfigurableSignalHandler<TSignal, THandler> c)
        {
            return c.WithPublisher(configurePublisher);
        }

        throw new ArgumentException($"handler type '{handler.GetType()}' is not supported in {nameof(WithPublisher)}", nameof(handler), null);
    }

    public static THandler WithPublisher<TSignal, THandler>(this ISignalHandler<TSignal, THandler> handler,
                                                            ConfigureSignalPublisherAsync<TSignal> configurePublisher)
        where TSignal : class, ISignal<TSignal>
        where THandler : class, ISignalHandler<TSignal, THandler>
    {
        if (handler is IConfigurableSignalHandler<TSignal, THandler> c)
        {
            return c.WithPublisher(configurePublisher);
        }

        throw new ArgumentException($"handler type '{handler.GetType()}' is not supported in {nameof(WithPublisher)}", nameof(handler), null);
    }
}
