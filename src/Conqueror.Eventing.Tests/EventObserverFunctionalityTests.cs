namespace Conqueror.Eventing.Tests
{
    public sealed class EventObserverFunctionalityTests
    {
        [Test]
        public async Task GivenEventWithPayload_ObserverReceivesEventWhenCalledDirectly()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorEventing()
                        .AddTransient<TestEventObserver>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();

            var evt = new TestEvent { Payload = 10 };

            await observer.HandleEvent(evt, CancellationToken.None);

            Assert.That(observations.Events, Is.EquivalentTo(new[] { evt }));
        }

        [Test]
        public async Task GivenEventWithPayload_ObserverReceivesEventWhenCalledViaPublisher()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorEventing()
                        .AddTransient<TestEventObserver>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var publisher = provider.GetRequiredService<IEventPublisher>();

            var evt = new TestEvent { Payload = 10 };

            await publisher.PublishEvent(evt, CancellationToken.None);

            Assert.That(observations.Events, Is.EquivalentTo(new[] { evt }));
        }

        [Test]
        public async Task GivenCancellationToken_ObserverReceivesCancellationTokenWhenCalledDirectly()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorEventing()
                        .AddTransient<TestEventObserver>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
            using var tokenSource = new CancellationTokenSource();

            await observer.HandleEvent(new() { Payload = 2 }, tokenSource.Token);

            Assert.That(observations.CancellationTokens, Is.EquivalentTo(new[] { tokenSource.Token }));
        }

        [Test]
        public async Task GivenCancellationToken_ObserverReceivesCancellationTokenWhenCalledViaPublisher()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorEventing()
                        .AddTransient<TestEventObserver>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var publisher = provider.GetRequiredService<IEventPublisher>();
            using var tokenSource = new CancellationTokenSource();

            await publisher.PublishEvent(new TestEvent { Payload = 2 }, tokenSource.Token);

            Assert.That(observations.CancellationTokens, Is.EquivalentTo(new[] { tokenSource.Token }));
        }

        [Test]
        public async Task GivenEventTypeWithoutRegisteredObserver_DirectlyResolvedObserverIsNoop()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorEventing()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();

            await observer.HandleEvent(new() { Payload = 10 }, CancellationToken.None);

            Assert.That(observations.Events, Is.Empty);
        }

        [Test]
        public async Task GivenEventTypeWithoutRegisteredObserver_PublishingEventLeadsToNoop()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorEventing()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var publisher = provider.GetRequiredService<IEventPublisher>();

            await publisher.PublishEvent(new TestEvent { Payload = 10 }, CancellationToken.None);

            Assert.That(observations.Events, Is.Empty);
        }

        [Test]
        public void GivenObserverWithInvalidInterface_RegisteringObserverThrowsArgumentException()
        {
            _ = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddConquerorEventing().AddTransient<TestEventObserverWithoutValidInterfaces>().FinalizeConquerorRegistrations());
            _ = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddConquerorEventing().AddScoped<TestEventObserverWithoutValidInterfaces>().FinalizeConquerorRegistrations());
            _ = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddConquerorEventing().AddSingleton<TestEventObserverWithoutValidInterfaces>().FinalizeConquerorRegistrations());
        }

        private sealed record TestEvent
        {
            public int Payload { get; init; }
        }

        private sealed class TestEventObserver : IEventObserver<TestEvent>
        {
            private readonly TestObservations responses;

            public TestEventObserver(TestObservations responses)
            {
                this.responses = responses;
            }

            public async Task HandleEvent(TestEvent evt, CancellationToken cancellationToken)
            {
                await Task.Yield();
                responses.Events.Add(evt);
                responses.CancellationTokens.Add(cancellationToken);
            }
        }

        private sealed class TestEventObserverWithoutValidInterfaces : IEventObserver
        {
        }

        private sealed class TestObservations
        {
            public List<TestEvent> Events { get; } = new();

            public List<CancellationToken> CancellationTokens { get; } = new();
        }
    }
}
