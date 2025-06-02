using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public static class AggregateSignalPublisherBuilderExtensions
{
    public static IAggregateSignalPublisher<TSignal> UseAggregate<TSignal>(
        this ISignalPublisherBuilder<TSignal> builder,
        ISignalPublisher<TSignal> firstPublisher,
        ISignalPublisher<TSignal> secondPublisher,
        params ISignalPublisher<TSignal>[] additionalPublishers)
        where TSignal : class, ISignal<TSignal>
        => builder.UseAggregate([firstPublisher, secondPublisher, ..additionalPublishers]);

    public static IAggregateSignalPublisher<TSignal> UseAggregate<TSignal>(
        this ISignalPublisherBuilder<TSignal> builder,
        IReadOnlyCollection<ISignalPublisher<TSignal>> publishers)
        where TSignal : class, ISignal<TSignal>
    {
        if (builder.ServiceProvider.GetService(typeof(IAggregateSignalPublisherFactory)) is not IAggregateSignalPublisherFactory publisherFactory)
        {
            throw new InvalidOperationException(
                $"could not resolve '{typeof(IAggregateSignalPublisherFactory)}'; did you forget to add Conqueror to the service collection?");
        }

        return publisherFactory.Create(publishers);
    }
}
