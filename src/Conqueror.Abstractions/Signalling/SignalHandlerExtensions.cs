using System;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public static class SignalHandlerExtensions
{
    public static TIHandler WithPipeline<TSignal, TIHandler>(this ISignalHandler<TSignal, TIHandler> handler,
                                                             Action<ISignalPipeline<TSignal>> configurePipeline)
        where TSignal : class, ISignal<TSignal>
        where TIHandler : class, ISignalHandler<TSignal, TIHandler>
    {
        if (handler is ISignalHandlerProxy<TSignal, TIHandler> c)
        {
            return c.WithPipeline(configurePipeline);
        }

        throw new ArgumentException($"handler type '{handler.GetType()}' is not supported in {nameof(WithPipeline)}", nameof(handler), null);
    }

    public static TIHandler WithTransport<TSignal, TIHandler>(this ISignalHandler<TSignal, TIHandler> handler,
                                                              ConfigureSignalPublisher<TSignal> configureTransport)
        where TSignal : class, ISignal<TSignal>
        where TIHandler : class, ISignalHandler<TSignal, TIHandler>
    {
        if (handler is ISignalHandlerProxy<TSignal, TIHandler> c)
        {
            return c.WithTransport(configureTransport);
        }

        throw new ArgumentException($"handler type '{handler.GetType()}' is not supported in {nameof(WithTransport)}", nameof(handler), null);
    }

    public static TIHandler WithTransport<TSignal, TIHandler>(this ISignalHandler<TSignal, TIHandler> handler,
                                                              ConfigureSignalPublisherAsync<TSignal> configureTransport)
        where TSignal : class, ISignal<TSignal>
        where TIHandler : class, ISignalHandler<TSignal, TIHandler>
    {
        if (handler is ISignalHandlerProxy<TSignal, TIHandler> c)
        {
            return c.WithTransport(configureTransport);
        }

        throw new ArgumentException($"handler type '{handler.GetType()}' is not supported in {nameof(WithTransport)}", nameof(handler), null);
    }
}
