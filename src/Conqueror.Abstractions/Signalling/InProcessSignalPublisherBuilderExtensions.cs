using System;

// ReSharper disable once CheckNamespace (we want these extensions to be accessible from client registration code without an extra import)
namespace Conqueror;

public static class InProcessSignalPublisherBuilderExtensions
{
    public static IInProcessSignalPublisher<TSignal> UseInProcess<TSignal>(
        this ISignalPublisherBuilder<TSignal> builder)
        where TSignal : class, ISignal<TSignal>
    {
        if (builder.ServiceProvider.GetService(typeof(IInProcessSignalPublisherFactory)) is not IInProcessSignalPublisherFactory publisherFactory)
        {
            throw new InvalidOperationException(
                $"could not resolve '{typeof(IInProcessSignalPublisherFactory)}'; did you forget to add Conqueror to the service collection?");
        }

        return publisherFactory.Get<TSignal>();
    }
}
