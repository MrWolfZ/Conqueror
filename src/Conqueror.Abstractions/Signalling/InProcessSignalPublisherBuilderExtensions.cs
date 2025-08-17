namespace Conqueror;

public static class InProcessSignalPublisherBuilderExtensions
{
    public static IInProcessSignalPublisher<TSignal> UseInProcess<TSignal>(this SignalPublisherBuilder<TSignal> builder)
        where TSignal : class, ISignal<TSignal>
    {
        if (
            builder.ServiceProvider.GetService(typeof(IInProcessSignalPublisherFactory))
            is not IInProcessSignalPublisherFactory publisherFactory
        )
        {
            throw new InvalidOperationException(
                $"could not resolve '{typeof(IInProcessSignalPublisherFactory)}'; did you forget to add Conqueror to the service collection?"
            );
        }

        return publisherFactory.Get<TSignal>();
    }
}
