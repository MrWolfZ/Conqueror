namespace Conqueror.Tests.Signalling;

public sealed partial class AggregateSignalPublisherTests
{
    // note that we are only testing the configuration builder methods here and not the concrete
    // strategy implementations since those are already tested in the strategy tests
    [Test]
    [Combinatorial]
    public async Task GivenConfiguredBroadcastingStrategy_WhenPublishingSignal_ConfiguredStrategyIsUsed(
        [Values("default", "sequential", "sequentialWithConfig", "parallel", "parallelWithConfig", "custom")]
            string strategyType,
        [Values(arg1: true, arg2: false)] bool nestedPublisherThrowsException,
        [Values(arg1: true, arg2: false)] bool passPublishersAsEnumerable
    )
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var exception = new Exception();

        _ = services.AddConqueror().AddSingleton<TestBroadcastingStrategy>().AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider
            .GetRequiredService<ISignalPublishers>()
            .For(TestSignal.T)
            .WithTransport(p =>
            {
                var obs = p.ServiceProvider.GetRequiredService<TestObservations>();

                var publisher = passPublishersAsEnumerable
                    ? p.UseAggregate(
                        [
                            new TestSignalPublisher1<TestSignal>(
                                obs,
                                nestedPublisherThrowsException ? exception : null
                            ),
                            new TestSignalPublisher2<TestSignal>(obs),
                            new TestSignalPublisher2<TestSignal>(obs),
                        ]
                    )
                    : p.UseAggregate(
                        new TestSignalPublisher1<TestSignal>(obs, nestedPublisherThrowsException ? exception : null),
                        new TestSignalPublisher2<TestSignal>(obs),
                        new TestSignalPublisher2<TestSignal>(obs)
                    );

                var transportType = new SignalTransportType(publisher.TransportTypeName, SignalTransportRole.Publisher);
                Assert.That(transportType.IsAggregate(), Is.True);
                Assert.That(transportType.IsAggregate(out var innerTypes), Is.True);
                Assert.That(innerTypes, Is.EqualTo(["test1", "test2", "test2"]));
                Assert.That(
                    publisher.TransportTypeName,
                    Is.EqualTo($"{ConquerorConstants.AggregateTransportName}[test1,test2,test2]")
                );

                return strategyType switch
                {
                    "default" => publisher,
                    "sequential" => publisher.WithSequentialBroadcastingStrategy(),
                    "sequentialWithConfig" => publisher.WithSequentialBroadcastingStrategy(c =>
                        c.WithThrowOnFirstException()
                    ),
                    "parallel" => publisher.WithParallelBroadcastingStrategy(),
                    "parallelWithConfig" => publisher.WithParallelBroadcastingStrategy(c =>
                        c.WithMaxDegreeOfParallelism(value: null)
                    ),
                    "custom" => publisher.WithBroadcastingStrategy(
                        p.ServiceProvider.GetRequiredService<TestBroadcastingStrategy>()
                    ),
                    _ => throw new ArgumentOutOfRangeException(nameof(strategyType), strategyType, message: null),
                };
            });

        var signal = new TestSignal { Payload = 10 };

        if (nestedPublisherThrowsException)
        {
            if (
                string.Equals(strategyType, "parallel", StringComparison.Ordinal)
                || string.Equals(strategyType, "parallelWithConfig", StringComparison.Ordinal)
            )
            {
                await Assert.ThatAsync(
                    () => handler.Handle(signal, CancellationToken.None),
                    Throws.InstanceOf<AggregateException>().With.InnerException.SameAs(exception)
                );
            }
            else
            {
                await Assert.ThatAsync(
                    () => handler.Handle(signal, CancellationToken.None),
                    Throws.Exception.SameAs(exception)
                );
            }
        }
        else
        {
            await handler.Handle(signal, CancellationToken.None);

            Assert.That(
                observations.ObservedPublisherExecutions,
                Is.EquivalentTo(
                    new[]
                    {
                        (typeof(TestSignalPublisher1<TestSignal>), signal),
                        (typeof(TestSignalPublisher2<TestSignal>), signal),
                        (typeof(TestSignalPublisher2<TestSignal>), signal),
                    }
                )
            );

            if (string.Equals(strategyType, "custom", StringComparison.Ordinal))
            {
                Assert.That(observations.ObservedCustomStrategyExecutions, Is.EqualTo([signal]));
            }
        }
    }

    [Signal]
    private sealed partial record TestSignal
    {
        public int Payload { get; init; }
    }

    private sealed class TestSignalPublisher1<TSignal>(
        TestObservations observations,
        Exception? exceptionToThrow = null
    ) : ISignalPublisher<TSignal>
        where TSignal : class, ISignal<TSignal>
    {
        public string TransportTypeName => "test1";

        public async Task Publish(
            TSignal signal,
            IServiceProvider serviceProvider,
            ConquerorContext conquerorContext,
            CancellationToken cancellationToken
        )
        {
            await Task.Yield();

            observations.ObservedPublisherExecutions.Enqueue((GetType(), signal));

            if (exceptionToThrow is not null)
            {
                throw exceptionToThrow;
            }
        }
    }

    private sealed class TestSignalPublisher2<TSignal>(TestObservations observations) : ISignalPublisher<TSignal>
        where TSignal : class, ISignal<TSignal>
    {
        public string TransportTypeName => "test2";

        public async Task Publish(
            TSignal signal,
            IServiceProvider serviceProvider,
            ConquerorContext conquerorContext,
            CancellationToken cancellationToken
        )
        {
            await Task.Yield();

            observations.ObservedPublisherExecutions.Enqueue((GetType(), signal));
        }
    }

    private sealed class TestBroadcastingStrategy(TestObservations observations) : ISignalBroadcastingStrategy
    {
        public async Task BroadcastSignal<TSignal>(
            IReadOnlyCollection<SignalHandlerFn<TSignal>> signalHandlerInvocationFns,
            IServiceProvider serviceProvider,
            TSignal signal,
            CancellationToken cancellationToken
        )
            where TSignal : class, ISignal<TSignal>
        {
            observations.ObservedCustomStrategyExecutions.Enqueue(signal);

            foreach (var fn in signalHandlerInvocationFns)
            {
                await fn(signal, serviceProvider, cancellationToken);
            }
        }
    }

    private sealed class TestObservations
    {
        public ConcurrentQueue<object> ObservedCustomStrategyExecutions { get; } = [];

        public ConcurrentQueue<(Type PublisherType, object Signal)> ObservedPublisherExecutions { get; } = [];
    }
}
