namespace Quickstart.Enhanced;

using Conqueror;

public static class SignalPublisherBuilderExtensions
{
    public static ISignalPublisher<TSignal> UseInProcessAndServerSentEvents<TSignal>(
        this SignalPublisherBuilder<TSignal> builder
    )
        where TSignal : class, IHttpSseSignal<TSignal> =>
        builder.UseAggregate(builder.UseInProcess(), builder.UseHttpServerSentEvents());

    public static TIHandler WithInProcessAndServerSentEventsTransport<TSignal, TIHandler>(
        this ISignalHandler<TSignal, TIHandler> handler
    )
        where TSignal : class, IHttpSseSignal<TSignal>
        where TIHandler : class, IHttpSseSignalHandler<TSignal, TIHandler> =>
        handler.WithTransport(UseInProcessAndServerSentEvents);
}
