using Conqueror;

namespace Quickstart.Enhanced;

public static class SignalPublisherBuilderExtensions
{
    public static ISignalPublisher<TSignal> UseInProcessAndServerSentEvents<TSignal>(
        this ISignalPublisherBuilder<TSignal> builder)
        where TSignal : class, IHttpSseSignal<TSignal>
    {
        return builder.UseAggregate(builder.UseInProcess(), builder.UseHttpServerSentEvents());
    }

    public static TIHandler WithInProcessAndServerSentEventsTransport<TSignal, TIHandler>(
        this ISignalHandler<TSignal, TIHandler> handler)
        where TSignal : class, IHttpSseSignal<TSignal>
        where TIHandler : class, IHttpSseSignalHandler<TSignal, TIHandler>
    {
        return handler.WithTransport(b => b.UseInProcessAndServerSentEvents());
    }
}
